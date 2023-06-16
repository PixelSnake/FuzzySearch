using FuzzyProductSearch.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace FuzzyProductSearch.Persistence
{
    /// <summary>
    /// Persists strings with a reference to their owner id, by which they can later be retrieved
    /// </summary>
    public class ItemStore<TItem> : IDisposable
        where TItem : IIdentifiable, ISerializable, new()
    {
        private string _name;
        private string _filename;
        private BinaryReader _reader;
        private BinaryWriter _writer;

        private Dictionary<ulong, long> _stringPositionIndex = new Dictionary<ulong, long>();
        private Dictionary<ulong, TItem> _unsavedValues = new Dictionary<ulong, TItem>();
        private long _stringPosition = 0;

        private const int HeaderLength = sizeof(short);
        private int _stringCount;

        private bool _batchMode = false;

        public ItemStore(string name)
        {
            _name = name;
            _filename = _name;

            Open();
        }

        public void StartBatch() => _batchMode = true;
        public void CommitBatch()
        {
            _batchMode = false;
            Write();
        }

        private void Open()
        {
            var source = File.Open(_filename, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Write);
            var dest = File.Open(_filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);

            _reader = new BinaryReader(source);
            _writer = new BinaryWriter(dest);
        }

        private void Close()
        {
            _reader.Close();
            _writer.Close();
        }

        public void Add(TItem item)
        {
            _unsavedValues.Add(item.Id, item);

            if (!_batchMode)
            {
                Write();
            }
        }

        private void Write()
        {
            WriteData();
            WriteIndex();
        }

        private void WriteData()
        {
            foreach (var (id, item) in _unsavedValues)
            {
                var posBefore = _writer.BaseStream.Position;
                item.Serialize(_writer);

                var length = _writer.BaseStream.Position - posBefore;
                _stringPosition += length;
                _stringPositionIndex[id] = _stringPosition;
            }

            _unsavedValues.Clear();
        }

        private void WriteIndex()
        {
            using var indexStream = new FileStream(_filename + "_index", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            var indexWriter = new BinaryWriter(indexStream);

            foreach (var (id, pos) in _stringPositionIndex)
            {
                indexWriter.Write(id);
                indexWriter.Write(pos); // position of the item in the data file
            }

            indexStream.Close();
        }

        public TItem GetValue(ulong id)
        {
            var pos = _stringPositionIndex[id];
            _reader.BaseStream.Seek(pos + HeaderLength, SeekOrigin.Begin);

            var item = new TItem();
            item.Deserialize(_reader);
            return item;
        }

        public IEnumerable<TItem> Values()
        {
            _reader.BaseStream.Seek(0, SeekOrigin.Begin);

            while (_reader.BaseStream.CanRead && _reader.BaseStream.Length > _reader.BaseStream.Position + sizeof(ulong))
            {
                var item = new TItem();
                item.Deserialize(_reader);
                yield return item;
            }
        }

        public void Dispose()
        {
            Close();
            _reader.Dispose();
            _writer.Dispose();
        }
    }
}
