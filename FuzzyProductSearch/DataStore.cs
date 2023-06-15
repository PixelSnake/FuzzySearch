using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FuzzyProductSearch.Attributes;
using FuzzyProductSearch.Exceptions;
using FuzzyProductSearch.Query;

namespace FuzzyProductSearch
{
    public class DataStore<TItem> 
        where TItem : IIdentifiable
    {
        public int Count => _itemsById.Count;
        
        private readonly SortedList<ulong, TItem> _itemsById = new SortedList<ulong, TItem>();
        private readonly Dictionary<string, SortedList<string, List<ulong>>> _itemsSortedBySearchableMembers = new Dictionary<string, SortedList<string, List<ulong>>>();

        private readonly Dictionary<ulong, (int, int)> _searchHits = new Dictionary<ulong, (int, int)>();

        /// <summary>
        /// All properties of TItem that are marked with the Fuzzy attribute
        /// </summary>
        private readonly Dictionary<string, PropertyInfo> _fuzzySearchableProperties = new Dictionary<string, PropertyInfo>();
        /// <summary>
        /// Maps the item index to a list of all the values of all properties split by white space
        /// </summary>
        private readonly Dictionary<ulong, string[][]> _indexedProps = new Dictionary<ulong, string[][]>();
        
        private readonly DistanceComputer _distanceComputer = new DistanceComputer(ComputeDistance, float.MaxValue);

        public DataStore()
        {
            var props = typeof(TItem).GetProperties()
                .Where(p => p.GetCustomAttribute<FuzzyAttribute>() != null)
                .Where(p => p.PropertyType == typeof(string))
                .ToArray();

            if (props.Length < 1)
            {
                throw new ArgumentException("Type TItem must have at least one member with the [Fuzzy] attribute. Only string properties are supported at this time.");
            }

            foreach (var prop in props)
            {
                _fuzzySearchableProperties.Add(prop.Name, prop);
                _itemsSortedBySearchableMembers.Add(prop.Name, new SortedList<string, List<ulong>>());
            }
        }

        public void Add(TItem item)
        {
            _itemsById.Add(item.Id, item);

            foreach (var prop in _fuzzySearchableProperties.Values)
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

        public IEnumerable<SearchResult> Query(string query)
        {
            var queryParts = new QueryBuilder().BuildQuery(query);
            IEnumerable<SearchResult> queryEnumerable = null;

            foreach (var part in queryParts)
            {
                switch (part)
                {
                    case QueryBuilder.SearchQueryPart search:
                        if (queryEnumerable != null)
                        {
                            throw new QueryException("Unexpected SEARCH statement");
                        }

                        queryEnumerable = Find(search.SearchString);
                        break;

                    case QueryBuilder.LimitQueryPart limit:
                        if (queryEnumerable == null)
                        {
                            throw new QueryException("Unexpected LIMIT statement");
                        }

                        queryEnumerable = queryEnumerable.Take(limit.Limit);
                        break;

                    case QueryBuilder.OffsetQueryPart offset:
                        if (queryEnumerable == null)
                        {
                            throw new QueryException("Unexpected OFFSET statement");
                        }

                        queryEnumerable = queryEnumerable.Skip(offset.Offset);
                        break;

                    case QueryBuilder.MaximumDistanceQueryPart maximumDistance:
                        DistanceComputer.DistanceCutoff = maximumDistance.MaximumDistance;
                        break;

                    case QueryBuilder.ReturnQueryPart returns:
                        //queryEnumerable = queryEnumerable.Select();
                        break;
                }
            }

            if (queryEnumerable == null)
            {
                throw new QueryException("Query is empty");
            }

            return queryEnumerable;
        }

        public IEnumerable<SearchResult> Find(string query)
        {
            _searchHits.Clear();

            var queryParts = query.Split(' ').Select(p => p.Trim()).ToArray();
            var cachedComputer = new CachedValueComputer<TItem, float>(item => ComputeDistance(item, queryParts));

            _itemsById.AsParallel().ForAll(pair => cachedComputer.Compute(pair.Value));

            return cachedComputer.SortedValues()
                .Where(p => p.Value != _distanceComputer.MaxValue && _searchHits.ContainsKey(p.Key))
                .Select(p => new SearchResult
                {
                    Item = _itemsById[p.Key],
                    Rank = p.Value,
                    BestMatchIndex = _searchHits[p.Key].Item1,
                    BestMatchLength = _searchHits[p.Key].Item2,
                });
        }

        private static float ComputeDistance(string a, string b)
        {
            float lastValue = 0;

            foreach (var intermediateValue in StringDistance.WeightedLevenshteinDistance(a, b))
            {
                lastValue = intermediateValue;

                // here we could stop the distance calculation prematurely, if we knew when to, but the Levenshtein algorithm doesn't support this
                //if (!DistanceComputer.IncludeInResults(intermediateValue, a, b))
                //{
                //    break;
                //}
            }

            return lastValue;
        }

        private float ComputeDistance(TItem item, string[] queryParts)
        {
            var result = 0f;

            foreach (var part in queryParts)
            {
                var dist = ComputeDistance(item, part);
                if (dist == _distanceComputer.MaxValue)
                {
                    return dist;
                }

                result += dist / queryParts.Length;
            }

            return result;
        }

        private float ComputeDistance(TItem item, string query)
        {
            var minDist = _distanceComputer.MaxValue;
            (int index, int length)? bestResult = null;

            var partIndexOffset = 0;
            for (int i = 0; i < _fuzzySearchableProperties.Count; i++)
            {
                var propParts = _indexedProps[item.Id][i];
                var dist = _distanceComputer.ComputeMultiDistance(propParts, query, out var partIndex, out var partLength);

                if (dist < minDist)
                {
                    minDist = dist;
                    bestResult = (partIndex + partIndexOffset, partLength);
                }

                partIndexOffset++;
            }

            if (bestResult != null)
            {
                lock (_searchHits)
                {
                    _searchHits[item.Id] = bestResult.Value;
                }
            }

            return minDist;
        }

        private string[][] IndexProps(TItem item)
        {
            var propParts = new List<string[]>();

            foreach (var prop in _fuzzySearchableProperties.Values)
            {
                var value = (string)prop.GetValue(item)!;
                propParts.Add(value.Split(' ').Select(x => x.Trim().ToLower()).ToArray());
            }

            return propParts.ToArray();
        }


        public struct SearchResult
        {
            public TItem Item;
            public float Rank;
            public int BestMatchIndex;
            public int BestMatchLength;
        }
    }
}
