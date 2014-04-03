using System;
using System.Runtime.Serialization;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Example.Domain.Commands;
using Spark.Example.Domain.Events;

namespace Spark.Example.Sagas
{
    [DataContract]
    public sealed class MoneyTransferSaga : Saga
    {
        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        [DataMember(Name = "s")]
        public Boolean MoneySent { get; private set; }

        [DataMember(Name = "r")]
        public Boolean MoneyReceived { get; private set; }

        [DataMember(Name = "c")]
        public Boolean MoneyRefunded { get; private set; }

        [DataMember(Name = "f")]
        public Guid FromAccountId { get; private set; }

        [DataMember(Name = "t")]
        public Guid ToAccountId { get; private set; }

        protected override void Configure(SagaConfiguration saga)
        {
            saga.CanStartWith((MoneyTransferSent e) => e.TransferId);
            saga.CanStartWith((MoneyTransferReceived e) => e.TransferId);
            saga.CanHandle((MoneyTransferRefunded e) => e.TransferId);
            saga.CanHandle((MoneyTransferFailed e) => e.TransferId);
            saga.CanHandle((Timeout e) => e.CorrelationId);
        }

        public void Handle(MoneyTransferSent e)
        {
            MoneySent = true;
            Amount = e.Amount;
            ToAccountId = e.ToAccountId;
            FromAccountId = e.FromAccountId;

            if (MoneyReceived)
            {
                MarkCompleted();
            }
            else
            {
                Publish(e.ToAccountId, new ReceiveMoneyTransfer(CorrelationId, FromAccountId, Amount));
                ScheduleTimeout(TimeSpan.FromDays(1));
            }
        }

        public void Handle(MoneyTransferReceived e)
        {
            MoneyReceived = true;
            Amount = e.Amount;
            ToAccountId = e.ToAccountId;
            FromAccountId = e.FromAccountId;

            if (MoneySent)
                MarkCompleted();
        }

        public void Handle(MoneyTransferRefunded e)
        {
            MarkCompleted();
        }

        public void Handle(MoneyTransferFailed e)
        {
            ClearTimeout();

            if (MoneySent && !MoneyReceived && !MoneyRefunded)
            {
                MoneyRefunded = true;
                Publish(FromAccountId, new RefundMoneyTransfer(CorrelationId, Amount));
            }
        }

        public void Handle(Timeout e)
        {
            if (MoneySent && !MoneyReceived && !MoneyRefunded)
            {
                MoneyRefunded = true;
                Publish(FromAccountId, new RefundMoneyTransfer(CorrelationId, Amount));
            }
        }
    }
}
