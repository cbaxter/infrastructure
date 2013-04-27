using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Logging;
using Spark.Infrastructure.Messaging;

namespace Spark.Infrastructure.Eventing
{
    public sealed class EventDispatcher : PipelineHook
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly ISendMessages<Event> messageSender;
        private readonly ICreateMessages messageFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="EventDispatcher"/>.
        /// </summary>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="messageSender">The message sender.</param>
        public EventDispatcher(ICreateMessages messageFactory, ISendMessages<Event> messageSender)
        {
            Verify.NotNull(messageFactory, "messageFactory");
            Verify.NotNull(messageSender, "messageSender");

            this.messageFactory = messageFactory;
            this.messageSender = messageSender;
        }

        public override void PostSave(CommandContext context, Commit commit)
        {
            //TODO: Implement...
        }
    }
}
