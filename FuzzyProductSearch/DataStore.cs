using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FuzzyProductSearch.Attributes;

namespace FuzzyProductSearch
{
    public class DataStore<TItem> 
        where TItem : IIdentifiable
    {
        public int Count => _itemsById.Count;
        
        private SortedList<ulong, TItem> _itemsById = new();
        private Dictionary<string, SortedList<string, List<ulong>>> _itemsSortedBySearchableMembers = new();

        private Dictionary<ulong, (int, int)> _searchHits = new();

        /// <summary>
        /// All properties of TItem that are marked with the Fuzzy attribute
        /// </summary>
        private PropertyInfo[] _fuzzySearchableProperties;
        /// <summary>
        /// Maps the item index to a list of all the values of all properties split by white space
        /// </summary>
        private Dictionary<ulong, string[][]> _indexedProps = new();

        public DataStore()
        {
            _fuzzySearchableProperties = typeof(TItem).GetProperties()
                .Where(p => p.GetCustomAttribute<FuzzyAttribute>() != null)
                .Where(p => p.PropertyType == typeof(string))
                .ToArray();

            if (_fuzzySearchableProperties.Length < 1)
            {
                throw new ArgumentException("Type TItem must have at least one member with the [Fuzzy] attribute. Only string properties are supported at this time.");
            }

            foreach (var prop in _fuzzySearchableProperties)
            {
                _itemsSortedBySearchableMembers.Add(prop.Name, new());
            }
        }

        public void Add(TItem item)
        {
            _itemsById.Add(item.Id, item);

            foreach (var prop in _fuzzySearchableProperties)
            {
                var memberIndex = _itemsSortedBySearchableMembers[prop.Name];
                var value = (string)prop.GetValue(item)!;
                if (!memberIndex.ContainsKey(value))
                {
                    memberIndex[value] = new List<ulong>();
                }
                memberIndex[value].Add(item.Id);
            }

            _indexedProps[item.Id] = IndexProps(item);
        }

        public IEnumerable<SearchResult> Find(string query)
        {
            _searchHits.Clear();

            var queryParts = query.Split(' ').Select(p => p.Trim()).ToArray();

            var cachedComputer = new CachedValueComputer<TItem, int>(item => ComputeDistance(item, queryParts));

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

        private int ComputeDistance(TItem item, string[] queryParts)
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

        private int ComputeDistance(TItem item, string query)
        {
            var minDist = int.MaxValue;

            var partIndexOffset = 0;
            for (int i = 0; i < _fuzzySearchableProperties.Length; i++)
            {
                var propParts = _indexedProps[item.Id][i];
                var dist = ComputeMultiDistance(propParts, query, out var partIndex, out var partLength);

                if (dist < minDist)
                {
                    minDist = dist;
                    _searchHits[item.Id] = (partIndex + partIndexOffset, partLength);
                }

                partIndexOffset++;
            }

            return minDist;
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

            bestMatchIndex = -1;
            bestMatchLength = 0;

            for (int i = 0; i < parts.Length; i++)
            {
                var dist = StringDistance.LevenshteinDistance(parts[i], query);
                if (dist < min && dist < Math.Max(parts[i].Length, query.Length))
                {
                    min = dist;

                    bestMatchIndex = i;
                    bestMatchLength = parts[i].Length;
                }
            }

            return min;
        }

        private string[][] IndexProps(TItem item)
        {
            var propParts = new List<string[]>();

            foreach (var prop in _fuzzySearchableProperties)
            {
                var value = (string)prop.GetValue(item)!;
                propParts.Add(value.Split(' ').Select(x => x.Trim().ToLower()).ToArray());
            }

            return propParts.ToArray();
        }


        public struct SearchResult
        {
            public TItem Item;
            public int Rank;
            public int BestMatchIndex;
            public int BestMatchLength;
        }
    }
}
