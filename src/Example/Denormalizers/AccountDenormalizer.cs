using Spark.Cqrs.Eventing.Mappings;
using Spark.Example.Domain.Events;

namespace Spark.Example.Denormalizers
{
    [EventHandler]
    public sealed class AccountDenormalizer
    {
        public void Handle(AccountOpened e)
        { }

        public void Handle(MoneyDeposited e)
        { }

        public void Handle(MoneyWithdrawn e)
        { }

        public void Handle(AccountClosed e)
        { }
    }
}
