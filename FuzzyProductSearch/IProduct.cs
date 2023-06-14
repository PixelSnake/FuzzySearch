using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyProductSearch
{
    public interface IProduct : IIdentifiable
    {
        public string Name { get; }
        public string Manufacturer { get; }

        public IIndexedProduct ToIndexed();
    }
}
