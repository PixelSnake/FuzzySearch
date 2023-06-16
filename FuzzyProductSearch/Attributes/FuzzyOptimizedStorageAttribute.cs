using System;
using System.Collections.Generic;
using System.Text;

namespace FuzzyProductSearch.Attributes
{
    public class FuzzyOptimizedStorageAttribute : Attribute
    {
        public string OptimizedFieldName;

        public FuzzyOptimizedStorageAttribute(string optimizedField)
        {
            OptimizedFieldName = optimizedField;
        }
    }
}
