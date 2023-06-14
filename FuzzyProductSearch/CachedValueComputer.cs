using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyProductSearch
{
    public class CachedValueComputer<T, TValue>
        where T : IIdentifiable
        where TValue : notnull
    {
        private Func<T, TValue> _valueCalculator;
        private Dictionary<ulong, TValue> _valueCache = new();
        private SortedList<TValue, List<ulong>> _sortedValues = new();

        public CachedValueComputer(Func<T, TValue> valueCalculator)
        {
            _valueCalculator = valueCalculator;
        }

        public TValue Compute(T item)
        {
            if (_valueCache.TryGetValue(item.Id, out var cachedValue))
            {
                return cachedValue;
            }

            var value = _valueCalculator(item);
            _valueCache.Add(item.Id, value);
            
            if (!_sortedValues.ContainsKey(value))
            {
                _sortedValues[value] = new List<ulong>();
            }
            _sortedValues[value].Add(item.Id);
            return value;
        }

        /// <summary>
        /// Returns all cached values sorted in increasing order of their values
        /// </summary>
        public IEnumerable<KeyValuePair<ulong, TValue>> Values()
        {
            foreach (var (value, ids) in _sortedValues)
            {
                foreach (var id in ids)
                {
                    yield return new KeyValuePair<ulong, TValue>(id, value);
                }
            }
        }
    }
}
