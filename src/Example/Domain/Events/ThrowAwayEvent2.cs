using System;
using System.Runtime.Serialization;
using Spark.Cqrs.Eventing;

namespace Spark.Example.Domain.Events
{
    [DataContract] //TODO: DELETE - Temporary event to test Saga code (need to create proper example).
    public sealed class ThrowAwayEvent2 : Event
    {
        [IgnoreDataMember]
        public Guid ClientId { get { return AggregateId; } }

        [DataMember(Name = "n")]
        public String Name { get; private set; }

        public ThrowAwayEvent2(String name)
        {
            Verify.NotNullOrWhiteSpace(name, "name");

            Name = name;
        }
    }
}
