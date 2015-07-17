using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SQLServerCache.Demo.TestModel
{
    [DataContract(Name = "Compound", Namespace = "Model")]
    class Compound : Recipe
    {
        [DataMember]
        public List<CompositionRow> CompositionRows { get; set; }
    }
}