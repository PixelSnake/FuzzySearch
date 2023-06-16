using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuzzyProductSearch.Attributes;
using FuzzyProductSearch.Persistence;

namespace FuzzyProductSearch
{
    [Serializable]
    public class Product : IIdentifiable, ISerializable
    {
        public ulong Id { get; set;  }
        [Fuzzy] [FuzzyOptimizedStorage(nameof(ManufacturerParts))]
        public string Manufacturer;
        [Fuzzy] [FuzzyOptimizedStorage(nameof(NameParts))]
        public string Name;

        public string[] ManufacturerParts;
        public string[] NameParts;

        public Product() { }

        public Product(ulong id, string name, string manufacturer)
        {
            Id = id;
            Name = name;
            Manufacturer = manufacturer;
        }

        public void Serialize(BinaryWriter writer)
        {
            ManufacturerParts = StringSerializer.SplitString(Manufacturer);
            NameParts = StringSerializer.SplitString(Name);

            writer.Write(Id);
            
            writer.Write(ManufacturerParts.Length);
            foreach (var part in ManufacturerParts)
            {
                writer.Write(part);
            }

            writer.Write(NameParts.Length);
            foreach (var part in NameParts)
            {
                writer.Write(part);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            Id = reader.ReadUInt64();

            var len = reader.ReadInt32();
            ManufacturerParts = new string[len];
            for (int i = 0; i < len; i++)
            {
                ManufacturerParts[i] = reader.ReadString();
            }

            len = reader.ReadInt32();
            NameParts = new string[len];
            for (int i = 0; i < len; i++)
            {
                NameParts[i] = reader.ReadString();
            }
        }
    }
}
