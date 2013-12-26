using Spark.Cqrs.Eventing.Mappings;
using Spark.Example.Domain.Events;

namespace Spark.Example.Denormalizers
{
    [EventHandler]
    public sealed class AccountDenormalizer
    {
        public void Handle(AccountOpened e)
        { }
    }
}
