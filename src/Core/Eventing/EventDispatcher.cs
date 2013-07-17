using System;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Logging;

namespace Spark.Infrastructure.Eventing
{
    public sealed class EventDispatcher : PipelineHook
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IStoreEvents eventStore;
        private readonly Boolean markDispatched;

        public EventDispatcher(IStoreEvents eventStore /*ICreateMessages messageFactory, ISendMessages<Event> messageSender*/)
            : this(eventStore, Settings.Eventstore)
        { }

        public EventDispatcher(IStoreEvents eventStore, IStoreEventSettings settings)
        {
            Verify.NotNull(eventStore, "eventStore");

            this.eventStore = eventStore;
            this.markDispatched = settings.MarkDispatched;

            EnsurePersistedCommitsDispatched();
        }

        private void EnsurePersistedCommitsDispatched()
        {
            if (!markDispatched) 
                return;

            foreach (var commit in eventStore.GetUndispatched())
            {
                Log.WarnFormat("Processing undispatched commit {0}", commit.Id);
                DispatchCommit(commit);
            }
        }

        public override void PostSave(Aggregate aggregate, Commit commit, Exception error)
        {
            if (commit != null && commit.Id.HasValue)
                DispatchCommit(commit);
        }

        private void DispatchCommit(Commit commit)
        {
            var commitId = commit.Id.GetValueOrDefault();
            var events = commit.Events;
            var count = events.Count;

            for (var i = 0; i < events.Count; i++)
            {
                //TODO: Implement 
            }

            if (markDispatched)
                eventStore.MarkDispatched(commitId);
        }
    }
}
