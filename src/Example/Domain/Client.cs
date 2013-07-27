using System;
using Spark.Cqrs.Domain;
using Spark.Example.Domain.Commands;
using Spark.Example.Domain.Events;

namespace Spark.Example.Domain
{
    public class Client : Aggregate
    {
        public String Name { get; private set; }

        public void Handle(RegisterClient command)
        {
            //TODO: Should consider enforcing new aggregate... CreateWith(...)

            Raise(new ClientRegistered(command.Name));
            Raise(new ThrowAwayEvent1(command.Name)); //TODO: DELETE - Temporary event to test Saga code (need to create proper example).
            Raise(new ThrowAwayEvent2(command.Name)); //TODO: DELETE - Temporary event to test Saga code (need to create proper example).
        }

        protected void Apply(ClientRegistered e)
        {
            Name = e.Name;
        }
    }
}
