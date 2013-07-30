using Spark.Cqrs.Eventing.Mappings;
using Spark.Example.Domain.Events;

namespace Spark.Example.Denormalizers
{
    [EventHandler]
    public sealed class ClientDenormalizer
    {
        public void Handle(ClientRegistered e)
        { }
    }
}
