using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyProductSearch
{
    public interface IIdentifiable
    {
        public ulong Id { get; set; }
    }
}
