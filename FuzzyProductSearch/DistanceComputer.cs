﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyProductSearch
{
    public class DistanceComputer
    {
        public float MaxValue { get; }

        /// <summary>
        /// The maximum distance that will be included in the results
        /// </summary>
        public static float DistanceCutoff { get; set; } = .5f;

        private readonly Func<string, string, float> _distanceComputer;

        public DistanceComputer(Func<string, string, float> distanceComputer, float maxValue)
        {
            _distanceComputer = distanceComputer;
            MaxValue = maxValue;
        }

        public float Compute(string part, string query) => _distanceComputer(part, query);

        /// <summary>
        /// Computes the minimum distance of all parts to the given query.
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="query">The query to test the input parts for.</param>
        /// <param name="bestMatchIndex">Returns the index of the best matching part. Undefined behavior in case the return value equals int.MaxValue.</param>
        /// <param name="bestMatchLength">Returns the length of the best matching part. Undefined behavior in case the return value equals int.MaxValue.</param>
        /// <returns></returns>
        public float ComputeMultiDistance(string[] parts, string query, out int bestMatchIndex, out int bestMatchLength)
        {
            var min = MaxValue;

            bestMatchIndex = -1;
            bestMatchLength = 0;

            for (int i = 0; i < parts.Length; i++)
            {
                var dist = Compute(parts[i], query);
                if (dist < min && IncludeInResults(dist, parts[i], query))
                {
                    min = dist;

                    bestMatchIndex = i;
                    bestMatchLength = parts[i].Length;
                }
            }

            return min;
        }

        public static bool IncludeInResults(float distance, string part, string query) => distance / query.Length < DistanceCutoff;
    }
}
