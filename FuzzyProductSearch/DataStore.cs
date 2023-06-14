using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyProductSearch
{
    public class DataStore<TItem, TIndexedItem> 
        where TItem : IProduct
        where TIndexedItem : IIndexedProduct
    {
        public int Count => _itemsById.Count;
        
        private SortedList<ulong, TIndexedItem> _itemsById = new();
        private SortedList<string, List<ulong>> _itemsByName = new();
        private SortedList<string, List<ulong>> _itemsByManufacturer = new();

        private Dictionary<ulong, (int, int)> _searchHits = new();

        public void Add(TItem item)
        {
            _itemsById.Add(item.Id, (TIndexedItem)item.ToIndexed());

            if (!_itemsByName.ContainsKey(item.Name))
            {
                _itemsByName[item.Name] = new List<ulong>();
            }
            _itemsByName[item.Name].Add(item.Id);


            if (!_itemsByManufacturer.ContainsKey(item.Manufacturer))
            {
                _itemsByManufacturer[item.Manufacturer] = new List<ulong>();
            }
            _itemsByManufacturer[item.Manufacturer].Add(item.Id);
        }

        public IEnumerable<SearchResult> Find(string query)
        {
            _searchHits.Clear();

            var queryParts = query.Split(' ').Select(p => p.Trim()).ToArray();

            var cachedComputer = new CachedValueComputer<TIndexedItem, int>(item => ComputeDistance(item, queryParts));

            var start = 0;
            var end = _itemsById.Count;

            while (start < end)
            {
                var middle = (start + end) / 2;
                var middleItem = _itemsById.GetValueAtIndex(middle);
                var distance = cachedComputer.Compute(middleItem);

                var leftHalf = (start + middle) / 2;
                var leftHalfDist = cachedComputer.Compute(_itemsById.GetValueAtIndex(leftHalf));

                var rightHalf = (middle + end) / 2;
                var rightHalfDist = cachedComputer.Compute(_itemsById.GetValueAtIndex(rightHalf));

                if (leftHalfDist < rightHalfDist)
                {
                    end = middle;
                }
                else if (rightHalfDist < leftHalfDist)
                {
                    start = middle;
                }
                else
                {
                    start++;
                }
            }

            return cachedComputer.Values()
                .Where(p => p.Value != int.MaxValue)
                .Select(p => new SearchResult
                {
                    Item = _itemsById[p.Key],
                    Rank = p.Value,
                    BestMatchIndex = _searchHits[p.Key].Item1,
                    BestMatchLength = _searchHits[p.Key].Item2,
                });
        }

        private int ComputeDistance(TIndexedItem item, string[] queryParts)
        {
            var result = 0f;

            foreach (var part in queryParts)
            {
                var dist = ComputeDistance(item, part);
                if (dist == int.MaxValue)
                {
                    return dist;
                }

                result += (float)dist / queryParts.Length;
            }

            return (int)result;
        }

        private int ComputeDistance(TIndexedItem item, string query)
        {
            var nameDist = ComputeMultiDistance(item.NameParts, query, out var namePartIndex, out var namePartLength);
            var manufacturerDist = ComputeMultiDistance(item.ManufacturerParts, query, out var manufacturerPartIndex, out var manufacturerPartLength);

            if (nameDist < manufacturerDist)
            {
                _searchHits[item.Id] = (namePartIndex, namePartLength);
                return nameDist;
            }
            else
            {
                _searchHits[item.Id] = (manufacturerPartIndex + item.NameParts.Length, manufacturerPartLength);
                return manufacturerDist;
            }
        }

        /// <summary>
        /// Computes the minimum distance of all parts to the given query.
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="query">The query to test the input parts for.</param>
        /// <param name="bestMatchIndex">Returns the index of the best matching part. Undefined behavior in case the return value equals int.MaxValue.</param>
        /// <param name="bestMatchLength">Returns the length of the best matching part. Undefined behavior in case the return value equals int.MaxValue.</param>
        /// <returns></returns>
        private int ComputeMultiDistance(string[] parts, string query, out int bestMatchIndex, out int bestMatchLength)
        {
            var min = int.MaxValue;
            var minPartLength = Math.Min(parts.Min(p => p.Length), query.Length);

            bestMatchIndex = -1;
            bestMatchLength = 0;

            for (int i = 0; i < parts.Length; i++)
            {
                var dist = StringDistance.LevenshteinDistance(parts[i], query);
                if (dist < min)
                {
                    min = dist;

                    bestMatchIndex = i;
                    bestMatchLength = parts[i].Length;
                }
            }

            if (min >= minPartLength)
            {
                return int.MaxValue;
            }

            return min;
        }


        public struct SearchResult
        {
            public TIndexedItem Item;
            public int Rank;
            public int BestMatchIndex;
            public int BestMatchLength;
        }
    }
}
