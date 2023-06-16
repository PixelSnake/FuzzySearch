using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyProductSearch
{
    public static class StringDistance
    {
        /// <summary>
        /// Computes the Lenvenshtein-Distance between two strings. Less is more similar.
        /// </summary>
        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                    return 0;
                return t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            return d[n, m];
        }

        /// <summary>
        /// Computes a weighten Levenshtein-Distance between two strings, where differences towards the beginning of a word are weighed stronger than distances towards the end of a word.
        /// Less is more similar.
        /// </summary>
        public static int WeightedLevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                {
                    return 0;
                }
                return t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int weight = m * n - i * j;
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    int weightedCost = cost * weight;
                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + weightedCost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }

            return d[m, n];
        }

        //public static IEnumerable<int> WeightedLevenshteinDistance2(string s, string t)
        //{
        //    int n = t.Length;
        //    int m = s.Length;

        //    var v0 = new int[n + 1];
        //    var v1 = new int[n + 1];

        //    for (int i = 0; i <= n; v0[i] = i++) ;

        //    for (int i = 0; i < m; i++)
        //    {
        //        v1[0] = i + 1;

        //        for (int j = 0; j < n; j++)
        //        {
        //            var deletionCost = v0[j + 1] + 1;
        //            var insertionCost = v1[j] + 1;
        //            var substitutionCost = v0[j] + (s[i] == t[j] ? 0 : 1);

        //            v1[j + 1] = Math.Min(deletionCost, Math.Min(insertionCost, substitutionCost));
        //        }
                
        //        Array.Copy(v1, v0, n + 1);
        //        yield return v0[n];
        //    }
        //}
    }
}
