using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SQLServerCache.Demo
{
    [DataContract]
    class Persons
    {
        [DataMember]
        public List<Person> Items = new List<Person>();
    }

    [DataContract]
    class Person
    {
        [DataMember]
        public int Age { get; set; }
        [DataMember]
        public string Name { get; set; }
    }
}