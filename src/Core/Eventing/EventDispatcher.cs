using System;
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

            //TODO: Pull any undispatched messages and dispatch (or at least have ready to dispatch -- have to review startup sequence to determine ordering)?
        }

        //TODO: PreSave should track a pending dispatch -- (i.e., prepare EventMessage and store in durable storage -- ESENT?) -- Maybe dispatch previous failures here ?
        //      Failure events are anything that made it to pre-dispatch state but not cleared out of system...

        public override void PostSave(Aggregate aggregate, Commit commit, Exception error)
        {
            //TODO: Mark events as predispatch so if fail here can at least potential duplicates etc (i.e., committed but not dispatched)???
            //TODO: Must handle save failure (i.e., cleanup).

            var count = commit.Events.Count;
            for (var i = 0; i < count; i++)
            {
                try
                {
                    //DispatchEvent(commit.StreamId, commit.Headers, commit.Events[i]);
                }
                catch (TimeoutException)
                {
                    Log.ErrorFormat("Failed to dispatch {0} of {1} events from commit {2}", count - i, count, commit.Id);
                    //TODO: Track failed commit, and specific event within commit that failed... 
                    //      perhaps serialize just events that weren't dispatched from commit  (i.e., convert to messages and serialize messages)? { Timestamp = commit.Timestamp, Headers = commit.Headers, Event = e}
                    //      Write out to local storage (file-system?, sqlce? something?).
                    throw;
                }
            }
        }

        //private void DispatchEvent(Guid aggregateId, HeaderCollection headers, Event e)
        //{
        //    var message = messageFactory.Create(new EventEnvelope(aggregateId, e), headers);
        //    var backoffContext = default(ExponentialBackoff);
        //    var done = false;

        //    do
        //    {
        //        try
        //        {
        //            messageSender.Send(message);
        //            done = true;
        //        }
        //        catch (Exception ex)
        //        {
        //            //if (backoffContext == null)
        //            //    backoffContext = new ExponentialBackoff(retryTimeout);

        //            //if (!backoffContext.CanRetry)
        //            //{
        //            //    //TODO: Write out to local storage (file system, sqlce, or something that is lightweight/durable -- local filesystem least likely to fail).
        //            //    throw new TimeoutException(Exceptions.DispatchTimeout.FormatWith(commit.CommitId, commit.StreamId), ex);
        //            //}

        //            Log.Warn(ex.Message);
        //            //backoffContext.WaitUntilRetry();
        //        }
        //    } while (!done);
        //}
    }
}
