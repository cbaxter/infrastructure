using System;
using Spark.Infrastructure;
using Spark.Infrastructure.Commanding;

namespace Example.Domain.Commands
{
    public abstract class ClientCommand : Command
    {
        public Guid ClientId { get; private set; }

        protected ClientCommand(Guid clientId)
        {
            Verify.NotEqual(Guid.Empty, clientId, "clientId");

            ClientId = clientId;
        }

        protected override Guid GetAggregateId()
        {
            return ClientId;
        }
    }

    public sealed class RegisterClient : ClientCommand
    {
        public String Name { get; private set; }

        public RegisterClient(Guid clientId, String name)
            : base(clientId)
        {
            Verify.NotNullOrWhiteSpace(name, "name");

            Name = name;
        }
    }
}
