using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SQLServerCache.Demo.TestModel
{
    [DataContract]
    class Recipes
    {
        [DataMember]
        public List<Recipe> Items = new List<Recipe>();
    }
}