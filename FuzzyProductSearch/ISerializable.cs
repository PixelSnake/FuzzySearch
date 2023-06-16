using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FuzzyProductSearch
{
    public interface ISerializable
    {
        public void Serialize(BinaryWriter writer);
        public void Deserialize(BinaryReader reader);
    }
}
