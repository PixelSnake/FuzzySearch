using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyProductSearch
{
    public class IndexedProduct : Product
    {
        public string[] NameParts { get; }
        public string[] ManufacturerParts {get; }

        public IndexedProduct(ulong id, string name, string manufacturer)
            : base(id, name, manufacturer)
        {
            NameParts = IndexString(name.ToLower());
            ManufacturerParts = IndexString(manufacturer.ToLower());
        }

        public IndexedProduct(Product p) : this(p.Id, p.Name, p.Manufacturer) { }

        private string[] IndexString(string str) => str.Split(' ').Select(x => x.Trim()).ToArray();
    }
}
