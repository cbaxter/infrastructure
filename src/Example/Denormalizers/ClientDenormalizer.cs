using Example.Domain.Events;
using Spark.Infrastructure.Eventing.Mappings;

namespace Example.Denormalizers
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
