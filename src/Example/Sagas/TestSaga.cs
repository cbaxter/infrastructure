using System;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Example.Domain.Events;

namespace Spark.Example.Sagas
{
    public sealed class TestSaga : Saga
    {
        protected override void Configure(SagaConfiguration saga)
        {
            saga.CanStartWith((ClientRegistered e) => e.ClientId);
            saga.CanHandle((ThrowAwayEvent1 e) => e.ClientId);
            saga.CanHandle((ThrowAwayEvent2 e) => e.ClientId);
        }

        public void Handle(ClientRegistered e)
        {
            ScheduleTimeout(TimeSpan.FromMilliseconds(10));
        }

        public void Handle(ThrowAwayEvent1 e)
        {

        }

        public void Handle(ThrowAwayEvent2 e)
        {
            //MarkCompleted();
        }

        protected override void OnTimeout(Timeout e)
        {
            MarkCompleted();
        }
    }
}
