using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FuzzyProductSearch.Persistence
{
    public static class StringSerializer
    {
        public static string[] SplitString(string s)
        {
            return s.Split(' ').Select(x => x.Trim().ToLower()).ToArray();
        }
    }
}
