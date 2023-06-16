using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FuzzyProductSearch.Attributes;
using FuzzyProductSearch.Exceptions;
using FuzzyProductSearch.Persistence;
using FuzzyProductSearch.Query;

namespace FuzzyProductSearch
{
    public class DataStore<TItem> 
        where TItem : IIdentifiable, ISerializable, new()
    {
        private readonly Dictionary<ulong, (int, int)> _searchHits = new Dictionary<ulong, (int, int)>();

        /// <summary>
        /// All properties of TItem that are marked with the Fuzzy attribute
        /// </summary>
        private readonly FieldInfo[] _fuzzySearchableProperties;
        private readonly Dictionary<FieldInfo, FieldInfo> _optimizedStorageField = new Dictionary<FieldInfo, FieldInfo>();
        private readonly ItemStore<TItem> _stringStore;
        
        private readonly DistanceComputer _distanceComputer = new DistanceComputer(ComputeDistance, float.MaxValue);

        public DataStore()
        {
            _fuzzySearchableProperties = typeof(TItem).GetFields()
                .Where(p => p.GetCustomAttribute<FuzzyAttribute>() != null)
                .Where(p => p.FieldType == typeof(string))
                .ToArray();

            var optimizedStorageFields = typeof(TItem).GetFields()
                .Where(p => p.FieldType == typeof(string))
                .Select(p => (field: p, attr: p.GetCustomAttribute<FuzzyOptimizedStorageAttribute>()))
                .Where(pair => pair.attr != null)
                .Select(pair => (field: pair.field, optimizedField: typeof(TItem).GetField(pair.attr!.OptimizedFieldName)))
                .Where(pair => pair.optimizedField != null && pair.optimizedField.FieldType == typeof(string[]))
                .ToArray();
            foreach (var fieldPair in optimizedStorageFields)
            {
                _optimizedStorageField[fieldPair.field] = fieldPair.optimizedField!;
            }

            if (_fuzzySearchableProperties.Length < 1)
            {
                throw new ArgumentException("Type TItem must have at least one member with the [Fuzzy] attribute. Only string properties are supported at this time.");
            }

            _stringStore = new ItemStore<TItem>("data");
        }

        public void StartBatch() => _stringStore.StartBatch();
        public void CommitBatch() => _stringStore.CommitBatch();

        public void Add(TItem item)
        {
            _stringStore.Add(item);
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

            Profiler.Profile("computing all values", () =>
            {
                _stringStore.Values().AsParallel().ForAll(value => cachedComputer.Compute(value));
            });

            return cachedComputer.SortedValues()
                .Where(p => p.Value != _distanceComputer.MaxValue && _searchHits.ContainsKey(p.Key))
                .Select(p => new SearchResult
                {
                    Id = p.Key,
                    Rank = p.Value,
                    BestMatchIndex = _searchHits[p.Key].Item1,
                    BestMatchLength = _searchHits[p.Key].Item2,
                });
        }

        private static float ComputeDistance(string a, string b)
        {
            return Quickenshtein.Levenshtein.GetDistance(a, b);
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

        private IEnumerable<string> GetItemValues(TItem item)
        {
            foreach (var prop in _fuzzySearchableProperties)
            {
                if (_optimizedStorageField.TryGetValue(prop, out var optiProp))
                {
                    foreach (var val in (string[])optiProp.GetValue(item)!)
                    {
                        yield return val;
                    }
                }
                else
                {
                    foreach (var val in StringSerializer.SplitString((string)prop.GetValue(item)!))
                    {
                        yield return val;
                    }
                }
            }
        }

        private float ComputeDistance(TItem item, string query)
        {
            var minDist = _distanceComputer.MaxValue;
            (int index, int length)? bestResult = null;

            var partIndexOffset = 0;
            for (int i = 0; i < _fuzzySearchableProperties.Length; i++)
            {
                var propParts = GetItemValues(item).ToArray();
                var dist = _distanceComputer.ComputeMultiDistance(propParts, query, out var partIndex, out var partLength);

                if (dist < minDist)
                {
                    minDist = dist;
                    bestResult = (partIndex + partIndexOffset, partLength);
                }

                if (minDist == 0)
                {
                    break;
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

        private string GetStringValue(object? obj) => (string)(obj ?? "\0");


        public struct SearchResult
        {
            public ulong Id;
            public float Rank;
            public int BestMatchIndex;
            public int BestMatchLength;
        }
    }
}
