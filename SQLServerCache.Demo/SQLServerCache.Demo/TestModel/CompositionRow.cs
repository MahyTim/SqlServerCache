using System.Runtime.Serialization;

namespace SQLServerCache.Demo.TestModel
{
    [DataContract]
    class CompositionRow
    {
        public decimal Amount { get; set; }
        public string Code { get; set; }
    }
}