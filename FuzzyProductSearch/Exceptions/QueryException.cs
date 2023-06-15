using System;
using System.Collections.Generic;
using System.Text;

namespace FuzzyProductSearch.Exceptions
{
    internal class QueryException : System.Exception
    {
        public QueryException(string message) : base(message) { }
    }
}
