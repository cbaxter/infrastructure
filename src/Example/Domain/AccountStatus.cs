using System.ComponentModel;

namespace Spark.Example.Domain
{
    public enum AccountStatus
    {
        [Description("New")]
        New,

        [Description("Open")]
        Opened,

        [Description("Closed")]
        Closed
    }
}
