using System;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Logging;
using Spark.Infrastructure.Messaging;
using Spark.Infrastructure.Resources;
using Spark.Infrastructure.Threading;

namespace Spark.Infrastructure.Domain
{
    public sealed class AggregateStore : IStoreAggregates
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IApplyEvents aggregateUpdater;
        private readonly IStoreSnapshots snapshotStore;
        private readonly IStoreEvents eventStore;
        private readonly Int32 snapshotInterval;
        private readonly TimeSpan retryTimeout;

        public AggregateStore(IApplyEvents aggregateUpdater, IStoreSnapshots snapshotStore, IStoreEvents eventStore)
            : this(aggregateUpdater, snapshotStore, eventStore, Settings.Default.AggregateStore)
        { }

        internal AggregateStore(IApplyEvents aggregateUpdater, IStoreSnapshots snapshotStore, IStoreEvents eventStore, IStoreAggregateSettings settings)
        {
            Verify.NotNull(settings, "settings");
            Verify.NotNull(eventStore, "eventStore");
            Verify.NotNull(snapshotStore, "snapshotStore");
            Verify.NotNull(aggregateUpdater, "aggregateUpdater");

            this.snapshotInterval = settings.SnapshotInterval;
            this.retryTimeout = settings.SaveRetryTimeout;
            this.aggregateUpdater = aggregateUpdater;
            this.snapshotStore = snapshotStore;
            this.eventStore = eventStore;
        }

        public Aggregate Get(Type aggregateType, Guid id)
        {
            Verify.NotNull(aggregateType, "aggregateType");
            Verify.TypeDerivesFrom(typeof(Aggregate), aggregateType, "aggregateType");

            var aggregate = GetOrCreate(aggregateType, id);
            var commits = eventStore.GetStreamFrom(id, aggregate.Version);

            foreach (var commit in commits)
                ApplyCommitToAggregate(commit, aggregate);

            return aggregate;
        }

        private Aggregate GetOrCreate(Type aggregateType, Guid id)
        {
            Snapshot snapshot = snapshotStore.GetLastSnapshot(id);
            Aggregate aggregate;

            if (snapshot == null)
            {
                aggregate = (Aggregate)Activator.CreateInstance(aggregateType);
                aggregate.Id = id;
            }
            else
            {
                aggregate = (Aggregate)snapshot.State;
                aggregate.Id = id;
                aggregate.Version = snapshot.Version;
            }

            return aggregate;
        }

        public Commit Save(Aggregate aggregate, CommandContext context)
        {
            Verify.NotNull(aggregate, "aggregate");
            Verify.NotNull(context, "context");

            var backoffContext = default(ExponentialBackoff);
            var commit = CreateCommit(aggregate, context);
            var done = false;

            do
            {
                try
                {
                    eventStore.SaveCommit(commit);
                    done = true;
                }
                catch (DuplicateCommitException)
                {
                    Log.WarnFormat("Duplicate commit: {0}", commit);
                    done = true;
                }
                catch (ConcurrencyException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (backoffContext == null)
                        backoffContext = new ExponentialBackoff(retryTimeout);

                    if (!backoffContext.CanRetry)
                        throw new TimeoutException(Exceptions.CommitTimeout.FormatWith(commit.CommitId, commit.StreamId), ex);

                    Log.Warn(ex.Message);
                    backoffContext.WaitUntilRetry();
                }
            } while (!done);

            // NOTE: Apply commit directly to existing aggregate. By default, each call to `Get` returns a new `Aggregate` instance.
            //       Should the caller hold on to a reference to `this` aggregate instance, it is their responsibility to gaurd against
            //       modifications (via `aggregate.Copy()` call) if they require an unaltered instance of `aggregate`.
            ApplyCommitToAggregate(commit, aggregate);

            // NOTE: We do not need multiple snapshots for a given aggregate; thus we will simply replace any existing snapshot if required.
            if (aggregate.Version > 0 && aggregate.Version % snapshotInterval == 0)
                snapshotStore.ReplaceSnapshot(new Snapshot(aggregate.Id, aggregate.Version, aggregate));

            return commit;
        }

        private static Commit CreateCommit(Aggregate aggregate, CommandContext context)
        {
            EventCollection events = context.GetRaisedEvents();
            HeaderCollection headers;

            if (aggregate.Version == 0)
            {
                var typeHeader = new Header(Header.Aggregate, aggregate.GetType(), checkReservedNames: false);

                headers = new HeaderCollection(context.Headers.Concat(typeHeader));
            }
            else
            {
                headers = context.Headers;
            }

            return new Commit(aggregate.Id, aggregate.Version + 1, context.CommandId, headers, events);
        }

        private void ApplyCommitToAggregate(Commit commit, Aggregate aggregate)
        {
            //TODO: EventContext
            aggregate.Version = commit.Version;
            foreach (var e in commit.Events)
                aggregateUpdater.Apply(e, aggregate);
        }
    }
}
