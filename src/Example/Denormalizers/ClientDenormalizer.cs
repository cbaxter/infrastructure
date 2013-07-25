using Spark.Infrastructure.Eventing.Mappings;
using Spark.Infrastructure.Example.Domain.Events;

namespace Spark.Infrastructure.Example.Denormalizers
{
    [EventHandler]
    public sealed class ClientDenormalizer
    {
        public void Handle(ClientRegistered e)
        {
            //TODO: Implement
        }
    }
}
