using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyProductSearch
{
    public class Product : IProduct
    {
        public ulong Id { get; }
        public string Name { get; }
        public string Manufacturer { get; }

        public Product(ulong id, string name, string manufacturer)
        {
            Id = id;
            Name = name;
            Manufacturer = manufacturer;
        }

        public IIndexedProduct ToIndexed() => new IndexedProduct(this);
    }
}
