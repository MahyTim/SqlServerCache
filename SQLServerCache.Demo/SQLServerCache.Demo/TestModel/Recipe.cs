using System.Runtime.Serialization;

namespace SQLServerCache.Demo.TestModel
{
    [DataContract(Name = "Recipe", Namespace = "Model")]
    [KnownType(typeof(Compound))]
    class Recipe
    {
        [DataMember]
        public string Code { get; set; }
        [DataMember]
        public string Description { get; set; }
    }
}