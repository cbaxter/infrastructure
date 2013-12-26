using System;
using System.Threading;
using Spark.Example.Domain;

namespace Spark.Example.Services
{
    public interface IGenerateAccountNumbers
    {
        Int64 GetAccountNumber(AccountType type);
    }
    public sealed class AccountNumberGenerator : IGenerateAccountNumbers
    {
        private Int64 savings = 1000000;
        private Int64 chequing = 2000000;

        public Int64 GetAccountNumber(AccountType type)
        {
            switch (type)
            {
                case AccountType.Chequing: return Interlocked.Increment(ref chequing);
                case AccountType.Saving: return Interlocked.Increment(ref savings);
                default: throw new NotSupportedException();
            }
        }
    }
}
