using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuzzyProductSearch.Attributes;

namespace FuzzyProductSearch
{
    public class Product : IIdentifiable
    {
        public ulong Id { get; }
        [Fuzzy] public string Manufacturer { get; }
        [Fuzzy] public string Name { get; }

        public Product(ulong id, string name, string manufacturer)
        {
            Id = id;
            Name = name;
            Manufacturer = manufacturer;
        }
    }
}
