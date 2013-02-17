using System;
using System.Linq;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Messaging;

namespace Spark.Infrastructure.Domain
{
    public static class AggregateRepositoryExtensions
    {
        public static TAggregate Get<TAggregate>(this IRetrieveAggregates aggregateRepository, Guid id)
            where TAggregate : Aggregate
        {
            Verify.NotNull(aggregateRepository, "aggregateRepository");

            return (TAggregate)aggregateRepository.Get(typeof(TAggregate), id);
        }
    }

    public interface IRetrieveAggregates
    {
        Aggregate Get(Type aggregateType, Guid id);
    }

    public interface IStoreAggregates : IRetrieveAggregates
    {
        void Save(Aggregate aggregate, CommandContext context);
    }

    public class AggregateRepository : IStoreAggregates
    {
        private readonly IApplyEvents aggregateUpdater;
        private readonly IStoreSnapshots snapshotStore;
        private readonly IStoreEvents eventStore;

        public AggregateRepository(IApplyEvents aggregateUpdater, IStoreSnapshots snapshotStore, IStoreEvents eventStore)
        {
            Verify.NotNull(aggregateUpdater, "aggregateUpdater");
            Verify.NotNull(snapshotStore, "snapshotStore");
            Verify.NotNull(eventStore, "eventStore");

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
            // make thread static context?... allows two threads to be stupid and track commands/events separately.
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
                aggregate.SnapshotVersion = snapshot.Version;
            }

            return aggregate;
        }

        public void Save(Aggregate aggregate, CommandContext context)
        {
            var events = context.GetRaisedEvents();
            var headers = aggregate.Version == 0 ? new HeaderCollection(context.Headers.Concat(new Header())) : context.Headers; //TODO: Set _a header as resvered header for aggregate type.
            var commit = new Commit(aggregate.Id, aggregate.Version + 1, context.CommandId, headers, events);
         

            //TODO: clone

            //TODO: Might schedule task on get... have all the right info there...
            //if ((aggregate.Version - aggregate.SnapshotVersion) > 50) //TODO: Config option
            //{
            //    //TODO: Confic options for threshold and save or replace behaviors
            //    snapshotStore.SaveSnapshot(new Snapshot(aggregate.Id, aggregate.Version, aggregate)); //TODO: Suppress concurrency exception etc
            //    aggregate.SnapshotVersion = aggregate.Version;
            //}

            eventStore.SaveCommit(commit);

            //TODO: Purge from cache if cached impl on concurrencyexception and then rethrow (doesn't app[ly here, but CachedAggregateRepository impl)
        }

        private void ApplyCommitToAggregate(Commit commit, Aggregate aggregate)
        {
            //TODO: EventContext
            aggregate.Version = commit.Version;

            foreach (var e in commit.Events)
            {
                aggregateUpdater.Apply(e, aggregate);
            }
        }
    }

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
