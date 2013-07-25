using System;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.Example.Domain.Commands;
using Spark.Infrastructure.Example.Domain.Events;

namespace Spark.Infrastructure.Example.Domain
{
    public class Client : Aggregate
    {
        public String Name { get; private set; }

        public void Handle(RegisterClient command)
        {
            //TODO: Should consider enforcing new aggregate... CreateWith(...)

            Raise(new ClientRegistered(command.Name));
        }

        protected void Apply(ClientRegistered e)
        {
            Name = e.Name;
        }
    }
}
