using System;
using System.Runtime.Serialization;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Example.Domain.Commands;
using Spark.Example.Domain.Events;
using Spark.Example.Services;

namespace Spark.Example.Domain
{
    [DataContract]
    public class Account : Aggregate
    {
        /// <summary>
        /// The account number.
        /// </summary>
        [DataMember(Name = "n")]
        public Int64 Number { get; private set; }

        /// <summary>
        /// The current account balance.
        /// </summary>
        [DataMember(Name = "b")]
        public Decimal Balance { get; private set; }

        /// <summary>
        /// The account type.
        /// </summary>
        [DataMember(Name = "t")]
        public AccountType Type { get; private set; }

        /// <summary>
        /// The current account status.
        /// </summary>
        [DataMember(Name = "s")]
        public AccountStatus Status { get; private set; }

        /// <summary>
        /// Return <value>true</value> if this aggregate must be explicitly created by another <see cref="Aggregate"/> instance (default); otherwise return <value>false</value>.
        /// </summary>
        [IgnoreDataMember]
        protected override Boolean RequiresExplicitCreate { get { return false; } }

        /// <summary>
        /// Return <value>true</value> if a <see cref="Command"/> instance of <paramref name="commandType"/> can create a 
        /// new aggregateinstance; otherwise return <value>false</value> (default = <value>false</value>).
        /// </summary>
        /// <param name="commandType">The command type attempting to create this aggregate instance.</param>
        protected override bool CanCreateAggregate(Type commandType)
        {
            return commandType == typeof(OpenAccount);
        }

        /// <summary>
        /// Handle open account request.
        /// </summary>
        /// <param name="command">The open account command.</param>
        /// <param name="accountNumberGenerator">The account number generator service.</param>
        public void Handle(OpenAccount command, IGenerateAccountNumbers accountNumberGenerator)
        {
            if (Status == AccountStatus.New)
            {
                Raise(new AccountOpened(command.AccountType, accountNumberGenerator.GetAccountNumber(command.AccountType), Decimal.Zero, AccountStatus.Opened));
            }
            else
            {
                Raise(new AccountAlreadyOpened(command.AccountType, Number, Balance, Status));
            }
        }

        /// <summary>
        /// Apply account opened state changes.
        /// </summary>
        /// <param name="e">The account opened event.</param>
        protected void Apply(AccountOpened e)
        {
            Type = e.AccountType;
            Number = e.AccountNumber;
            Balance = e.Balance;
            Status = e.Status;
        }

        /// <summary>
        /// Handle deposit money request.
        /// </summary>
        /// <param name="command">The deposit money command.</param>
        public void Handle(DepositMoney command)
        {
            if (Status == AccountStatus.Opened)
            {
                Raise(new MoneyDeposited(Type, Number, Balance + command.Amount, command.Amount));
            }
            else
            {
                Raise(new MoneyDepositFailed(Messages.AccountMustBeOpen.FormatWith(Status), command));
            }
        }

        /// <summary>
        /// Apply money deposited state changes.
        /// </summary>
        /// <param name="e">The money deposited event.</param>
        protected void Apply(MoneyDeposited e)
        {
            Balance = e.Balance;
        }

        /// <summary>
        /// Handle withdrawl money request.
        /// </summary>
        /// <param name="command">The withdrawl money command.</param>
        public void Handle(WithdrawlMoney command)
        {
            if (Status == AccountStatus.Opened)
            {
                if (Balance < command.Amount)
                {
                    Raise(new InsufficientFunds(Type, Number, Balance, command.Amount));
                }
                else
                {
                    Raise(new MoneyWithdrawn(Type, Number, Balance - command.Amount, command.Amount));
                }
            }
            else
            {
                Raise(new WithdrawlMoneyFailed(Messages.AccountMustBeOpen.FormatWith(Status), command));
            }
        }

        /// <summary>
        /// Apply money withdrawn state changes.
        /// </summary>
        /// <param name="e">The money withdrawn event.</param>
        protected void Apply(MoneyWithdrawn e)
        {
            Balance = e.Balance;
        }

        /// <summary>
        /// Handle send money transfer request.
        /// </summary>
        /// <param name="command">The money transfer command.</param>
        public void Handle(SendMoneyTransfer command)
        {
            if (Status == AccountStatus.Opened)
            {
                if (Balance < command.Amount)
                {
                    Raise(new InsufficientFunds(Type, Number, Balance, command.Amount));
                }
                else
                {
                    var balance = Balance - command.Amount;

                    Raise(new MoneyWithdrawn(Type, Number, balance, command.Amount));
                    Raise(new MoneyTransferSent(command.TransferId, command.ToAccountId, Number, balance, command.Amount));
                }
            }
            else
            {
                Raise(new MoneyTransferFailed(command.TransferId, Messages.AccountMustBeOpen.FormatWith(Status), command));
            }
        }

        /// <summary>
        /// Handle receive money transfer request.
        /// </summary>
        /// <param name="command">The receive money transfer command.</param>
        public void Handle(ReceiveMoneyTransfer command)
        {
            if (Status == AccountStatus.Opened)
            {
                var balance = Balance + command.Amount;

                Raise(new MoneyTransferReceived(command.TransferId, command.FromAccountId, Number, balance, command.Amount));
                Raise(new MoneyDeposited(Type, Number, balance, command.Amount));
            }
            else
            {
                Raise(new MoneyTransferFailed(command.TransferId, Messages.AccountMustBeOpen.FormatWith(Status), command));
            }
        }

        /// <summary>
        /// Handle refund money transfer request.
        /// </summary>
        /// <param name="command">The refund money transfer command.</param>
        public void Handle(RefundMoneyTransfer command)
        {
            if (Status == AccountStatus.Opened)
            {
                var balance = Balance + command.Amount;

                Raise(new MoneyTransferRefunded(command.TransferId, Number, balance, command.Amount));
                Raise(new MoneyDeposited(Type, Number, balance, command.Amount));
            }
            else
            {
                Raise(new MoneyTransferFailed(command.TransferId, Messages.AccountMustBeOpen.FormatWith(Status), command));
            }
        }
        
        /// <summary>
        /// Handle close account request.
        /// </summary>
        /// <param name="command">The close account command.</param>
        public void Handle(CloseAccount command)
        {
            if (Status == AccountStatus.Opened)
            {
                if (Balance > Decimal.Zero)
                {
                    Raise(new CloseAccountFailed(Messages.ZeroBalanceRequired.FormatWith(Balance), command));
                }
                else
                {
                    Raise(new AccountClosed(Type, Number, Balance, AccountStatus.Closed));
                }
            }
            else
            {
                Raise(new AccountAlreadyClosed(Type, Number));
            }
        }
        
        /// <summary>
        /// Apply account closed state changes.
        /// </summary>
        /// <param name="e">The account closed event.</param>
        protected void Apply(AccountClosed e)
        {
            Status = e.Status;
        }
    }
}
