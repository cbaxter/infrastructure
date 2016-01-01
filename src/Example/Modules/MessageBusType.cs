using System.ComponentModel;

namespace Spark.Example.Modules
{
    public enum MessageBusType
    {
        [Description("Inline")]
        Inline = 1,

        [Description("Optimistic")]
        Optimistic = 2,

        [Description("MSMQ")]
        MicrosoftMessageQueuing = 3
    }
}
