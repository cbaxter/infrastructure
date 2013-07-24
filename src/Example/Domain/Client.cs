using Example.Domain.Commands;
using Example.Domain.Events;
using Spark.Infrastructure.Domain;

namespace Example.Domain
{
    public sealed class Client : Aggregate
    {
        public void Handle(RegisterClient command)
        {
            //TODO: Should consider enforcing new aggregate... CreateWith(...)

            Raise(new ClientRegistered(command.Name));
        }
    }
}
