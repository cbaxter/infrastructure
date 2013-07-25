using Spark.Infrastructure.Domain;
using Spark.Infrastructure.Example.Domain.Commands;
using Spark.Infrastructure.Example.Domain.Events;

namespace Spark.Infrastructure.Example.Domain
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
