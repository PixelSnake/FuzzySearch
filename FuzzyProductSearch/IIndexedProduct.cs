using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyProductSearch
{
    public interface IIndexedProduct : IProduct
    {
        public string[] NameParts { get; }
        public string[] ManufacturerParts { get; }
    }
}
