using System;
using System.Runtime.Serialization;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Eventing;

namespace Spark.Example.Domain.Events
{
    [DataContract]
    public abstract class CommandFailed : Event
    {
        [DataMember(Name = "c")]
        public Command Command { get; private set; }

        [DataMember(Name = "r")]
        public String Reason { get; private set; }

        protected CommandFailed(String reason, Command command)
        {
            Verify.NotNull(command, nameof(command));
            Verify.NotNullOrWhiteSpace(reason, nameof(reason));

            Command = command;
            Reason = reason;
        }
    }
}
