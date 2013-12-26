using System;
using System.Runtime.Serialization;
using Spark.Cqrs.Commanding;

namespace Spark.Example.Domain.Commands
{
    [DataContract]
    public sealed class OpenAccount : Command
    {
        [DataMember(Name = "t")]
        public AccountType AccountType { get; private set; }

        public OpenAccount(AccountType accountType)
        {
            AccountType = accountType;
        }
    }

    [DataContract]
    public sealed class DepositMoney : Command
    {
        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        public DepositMoney(Decimal amount)
        {
            Amount = amount;
        }
    }

    [DataContract]
    public sealed class WithdrawlMoney : Command
    {
        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        public WithdrawlMoney(Decimal amount)
        {
            Amount = amount;
        }
    }

    [DataContract]
    public sealed class SendMoneyTransfer : Command
    {
        [DataMember(Name = "t")]
        public Guid TransferId { get; private set; }

        [DataMember(Name = "n")]
        public Guid ToAccountId { get; private set; }

        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        public SendMoneyTransfer(Guid transferId, Guid to, Decimal amount)
        {
            TransferId = transferId;
            ToAccountId = to;
            Amount = amount;
        }
    }

    [DataContract]
    public sealed class ReceiveMoneyTransfer : Command
    {
        [DataMember(Name = "t")]
        public Guid TransferId { get; private set; }

        [DataMember(Name = "n")]
        public Guid FromAccountId { get; private set; }

        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        public ReceiveMoneyTransfer(Guid transferId, Guid from, Decimal amount)
        {
            TransferId = transferId;
            FromAccountId = from;
            Amount = amount;
        }
    }

    [DataContract]
    public sealed class RefundMoneyTransfer : Command
    {
        [DataMember(Name = "t")]
        public Guid TransferId { get; private set; }

        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        public RefundMoneyTransfer(Guid transferId, Decimal amount)
        {
            TransferId = transferId;
            Amount = amount;
        }
    }

    [DataContract]
    public sealed class CloseAccount : Command
    { }
}
