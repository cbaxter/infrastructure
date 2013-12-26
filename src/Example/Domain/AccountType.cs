using System.ComponentModel;

namespace Spark.Example.Domain
{
    public enum AccountType
    {
        [Description("Unknown Account")]
        Unknown,

        [Description("Chequing Account")]
        Chequing,

        [Description("Savings Account")]
        Saving
    }
}
