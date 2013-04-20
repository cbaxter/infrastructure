using System;
using System.Threading;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Logging;
using Spark.Infrastructure.Messaging;
using Spark.Infrastructure.Threading;

namespace Spark.Infrastructure.Domain
{
    //TODO: Reminder when creating cached implementation, create ability to verify aggregate state before returning cached object... thus won't throw on violation of modifying state when it happens, but on next use...

    public class AggregateRepository : IStoreAggregates
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IApplyEvents aggregateUpdater;
        private readonly IStoreSnapshots snapshotStore;
        private readonly IStoreEvents eventStore;
        private readonly TimeSpan retryTimeout;

        public AggregateRepository(IApplyEvents aggregateUpdater, IStoreSnapshots snapshotStore, IStoreEvents eventStore)
            : this(aggregateUpdater, snapshotStore, eventStore, Settings.AggregateRepository.RetryTimeout)
        { }

        internal AggregateRepository(IApplyEvents aggregateUpdater, IStoreSnapshots snapshotStore, IStoreEvents eventStore, TimeSpan retryTimeout)
        {
            Verify.NotNull(aggregateUpdater, "aggregateUpdater");
            Verify.NotNull(snapshotStore, "snapshotStore");
            Verify.NotNull(eventStore, "eventStore");

            this.retryTimeout = retryTimeout;
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
                aggregate.SnapshotVersion = snapshot.Version; //TODO: Snapshot on load rather than save (might not be saved)?
            }

            return aggregate;
        }

        public void Save(Aggregate aggregate, CommandContext context)
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

                    if (backoffContext.CanRetry)
                    {
                        Log.Warn(ex.Message);
                        backoffContext.WaitUntilRetry();
                    }
                    else
                    {
                        Log.Error(ex);
                        done = true;
                    }
                }
            } while (!done);
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

        private void ApplyCommitToAggregate(Commit commit, Aggregate aggregate) //TODO: likely need to make protected
        {
            //TODO: EventContext

            aggregate.Version = commit.Version;

            foreach (var e in commit.Events)
            {
                aggregateUpdater.Apply(e, aggregate);
            }
        }
    }

    

    //TODO: Implement
    //public class HookableAggregateRepository : IStoreAggregates
    //{
    //    public Aggregate Get(Type aggregateType, Guid id)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Save(Aggregate aggregate, CommandContext context)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //TODO: Implement
    //TODO: Purge from cache if cached impl on concurrencyexception and then rethrow (doesn't apply here, but CachedAggregateRepository impl)
    //TODO: Cached impl must work on copy of aggregate
    //TODO: Cached impl should gaurd against illegal state modification
    //public class CacheableAggregateRepository : IStoreAggregates
    //{
    //    public Aggregate Get(Type aggregateType, Guid id)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Save(Aggregate aggregate, CommandContext context)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
