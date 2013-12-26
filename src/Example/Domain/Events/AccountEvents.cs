using System;
using System.Runtime.Serialization;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Eventing;

namespace Spark.Example.Domain.Events
{
    [DataContract]
    public sealed class AccountOpened : Event
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        [DataMember(Name = "t")]
        public AccountType AccountType { get; private set; }

        [DataMember(Name = "n")]
        public Int64 AccountNumber { get; private set; }

        [DataMember(Name = "b")]
        public Decimal Balance { get; private set; }

        [DataMember(Name = "s")]
        public AccountStatus Status { get; private set; }

        public AccountOpened(AccountType type, Int64 number, Decimal balance, AccountStatus status)
        {
            AccountType = type;
            AccountNumber = number;
            Balance = balance;
            Status = status;
        }
    }

    [DataContract]
    public sealed class AccountAlreadyOpened : Event
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        [DataMember(Name = "t")]
        public AccountType AccountType { get; private set; }

        [DataMember(Name = "n")]
        public Int64 AccountNumber { get; private set; }

        [DataMember(Name = "b")]
        public Decimal Balance { get; private set; }

        [DataMember(Name = "s")]
        public AccountStatus Status { get; private set; }

        public AccountAlreadyOpened(AccountType type, Int64 number, Decimal balance, AccountStatus status)
        {
            AccountType = type;
            AccountNumber = number;
            Balance = balance;
            Status = status;
        }
    }

    [DataContract]
    public sealed class MoneyDeposited : Event
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        [DataMember(Name = "t")]
        public AccountType AccountType { get; private set; }

        [DataMember(Name = "n")]
        public Int64 AccountNumber { get; private set; }

        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        [DataMember(Name = "b")]
        public Decimal Balance { get; private set; }

        public MoneyDeposited(AccountType type, Int64 number, Decimal balance, Decimal amount)
        {
            AccountType = type;
            AccountNumber = number;
            Balance = balance;
            Amount = amount;
        }
    }

    [DataContract]
    public sealed class MoneyDepositFailed : CommandFailed
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        public MoneyDepositFailed(String reason, Command command)
            : base(reason, command)
        { }
    }

    [DataContract]
    public sealed class MoneyWithdrawn : Event
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        [DataMember(Name = "t")]
        public AccountType AccountType { get; private set; }

        [DataMember(Name = "n")]
        public Int64 AccountNumber { get; private set; }

        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        [DataMember(Name = "b")]
        public Decimal Balance { get; private set; }

        public MoneyWithdrawn(AccountType type, Int64 number, Decimal balance, Decimal amount)
        {
            AccountType = type;
            AccountNumber = number;
            Balance = balance;
            Amount = amount;
        }
    }

    [DataContract]
    public sealed class InsufficientFunds : Event
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        [DataMember(Name = "t")]
        public AccountType AccountType { get; private set; }

        [DataMember(Name = "n")]
        public Int64 AccountNumber { get; private set; }

        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        [DataMember(Name = "b")]
        public Decimal Balance { get; private set; }

        public InsufficientFunds(AccountType type, Int64 number, Decimal balance, Decimal amount)
        {
            AccountType = type;
            AccountNumber = number;
            Balance = balance;
            Amount = amount;
        }
    }

    [DataContract]
    public sealed class WithdrawlMoneyFailed : CommandFailed
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        public WithdrawlMoneyFailed(String reason, Command command)
            : base(reason, command)
        { }
    }

    [DataContract]
    public sealed class MoneyTransferSent : Event
    {
        [DataMember(Name = "t")]
        public Guid TransferId { get; private set; }

        [IgnoreDataMember]
        public Guid FromAccountId { get { return AggregateId; } }

        [DataMember(Name = "d")]
        public Guid ToAccountId { get; private set; }

        [DataMember(Name = "n")]
        public Int64 AccountNumber { get; private set; }

        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        [DataMember(Name = "b")]
        public Decimal Balance { get; private set; }

        public MoneyTransferSent(Guid transferId, Guid accountId, Int64 number, Decimal balance, Decimal amount)
        {
            TransferId = transferId;
            ToAccountId = accountId;
            AccountNumber = number;
            Balance = balance;
            Amount = amount;
        }
    }

    [DataContract]
    public sealed class MoneyTransferReceived : Event
    {
        [DataMember(Name = "t")]
        public Guid TransferId { get; private set; }

        [DataMember(Name = "s")]
        public Guid FromAccountId { get; private set; }

        [IgnoreDataMember]
        public Guid ToAccountId { get { return AggregateId; } }

        [DataMember(Name = "n")]
        public Int64 AccountNumber { get; private set; }

        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        [DataMember(Name = "b")]
        public Decimal Balance { get; private set; }

        public MoneyTransferReceived(Guid transferId, Guid accountId, Int64 number, Decimal balance, Decimal amount)
        {
            TransferId = transferId;
            FromAccountId = accountId;
            AccountNumber = number;
            Balance = balance;
            Amount = amount;
        }
    }

    [DataContract]
    public sealed class MoneyTransferRefunded : Event
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        [DataMember(Name = "t")]
        public Guid TransferId { get; private set; }

        [DataMember(Name = "n")]
        public Int64 AccountNumber { get; private set; }

        [DataMember(Name = "a")]
        public Decimal Amount { get; private set; }

        [DataMember(Name = "b")]
        public Decimal Balance { get; private set; }

        public MoneyTransferRefunded(Guid transferId, Int64 number, Decimal balance, Decimal amount)
        {
            TransferId = transferId;
            AccountNumber = number;
            Balance = balance;
            Amount = amount;
        }
    }

    [DataContract]
    public sealed class MoneyTransferFailed : CommandFailed
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        [DataMember(Name = "t")]
        public Guid TransferId { get; private set; }

        public MoneyTransferFailed(Guid transferId, String reason, Command command)
            : base(reason, command)
        {
            TransferId = transferId;
        }
    }

    [DataContract]
    public sealed class AccountClosed : Event
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        [DataMember(Name = "t")]
        public AccountType AccountType { get; private set; }

        [DataMember(Name = "n")]
        public Int64 AccountNumber { get; private set; }

        [DataMember(Name = "b")]
        public Decimal Balance { get; private set; }

        [DataMember(Name = "s")]
        public AccountStatus Status { get; private set; }

        public AccountClosed(AccountType type, Int64 number, Decimal balance, AccountStatus status)
        {
            AccountType = type;
            AccountNumber = number;
            Balance = balance;
            Status = status;
        }
    }

    public sealed class AccountAlreadyClosed : Event
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        [DataMember(Name = "t")]
        public AccountType AccountType { get; private set; }

        [DataMember(Name = "n")]
        public Int64 AccountNumber { get; private set; }

        public AccountAlreadyClosed(AccountType type, Int64 number)
        {
            AccountType = type;
            AccountNumber = number;
        }
    }

    public sealed class CloseAccountFailed : CommandFailed
    {
        [IgnoreDataMember]
        public Guid AccountId { get { return AggregateId; } }

        public CloseAccountFailed(String reason, Command command)
            : base(reason, command)
        { }
    }
}
