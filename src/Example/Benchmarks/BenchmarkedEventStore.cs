using System;
using System.Collections.Generic;
using Spark.Cqrs.Eventing;
using Spark.EventStore;
using Spark.Messaging;

namespace Spark.Example.Benchmarks
{
    /// <summary>
    /// A event store decorator class to capture read/write operations.
    /// </summary>
    internal sealed class BenchmarkedEventStore : IStoreEvents
    {
        private readonly Statistics statistics;
        private readonly IStoreEvents eventStore;

        /// <summary>
        /// Initalizes a new isntance of <see cref="BenchmarkedEventStore"/>.
        /// </summary>
        /// <param name="eventStore">The event store to decorate.</param>
        /// <param name="statistics">The statistics class.</param>
        public BenchmarkedEventStore(IStoreEvents eventStore, Statistics statistics)
        {
            this.eventStore = eventStore;
            this.statistics = statistics;
        }

        /// <summary>
        /// Get all undispatched commits.
        /// </summary>
        public IEnumerable<Commit> GetUndispatched()
        {
            var result = eventStore.GetUndispatched();

            statistics.IncrementQueryCount();

            return result;
        }

        /// <summary>
        /// Get all known stream identifiers.
        /// </summary>
        /// <remarks>This method is not safe to call on an active event store; only use when new streams are not being committed.</remarks>
        public IEnumerable<Guid> GetStreams()
        {
            var result = eventStore.GetStreams();

            statistics.IncrementQueryCount();

            return result;
        }

        /// <summary>
        /// Get the specified commit sequence range starting after <paramref name="skip"/> and returing <paramref name="take"/> commits.
        /// </summary>
        /// <param name="skip">The commit sequence lower bound (exclusive).</param>
        /// <param name="take">The number of commits to include in the result.</param>
        public IReadOnlyList<Commit> GetRange(Int64 skip, Int64 take)
        {
            var result = eventStore.GetRange(skip, take);

            statistics.IncrementQueryCount();

            return result;
        }

        /// <summary>
        /// Gets all commits for the specified <paramref name="streamId"/> with a version greater than or equal to <paramref name="minimumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="minimumVersion">The minimum stream version (inclusive).</param>
        public IEnumerable<Commit> GetStream(Guid streamId, Int32 minimumVersion)
        {
            var result = eventStore.GetStream(streamId, minimumVersion);

            statistics.IncrementQueryCount();

            return result;
        }

        /// <summary>
        /// Deletes the specified event stream for <paramref name="streamId"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        public void DeleteStream(Guid streamId)
        {
            eventStore.DeleteStream(streamId);

            statistics.IncrementDeleteCount();
        }

        /// <summary>
        /// Appends a new commit to the event store.
        /// </summary>
        /// <param name="commit">The commit to append to the event store.</param>
        public void Save(Commit commit)
        {
            eventStore.Save(commit);

            if (commit.Version == 1)
                statistics.IncrementInsertCount();
            else
                statistics.IncrementUpdateCount();
        }

        /// <summary>
        /// Mark the specified commit as being dispatched.
        /// </summary>
        /// <param name="id">The unique commit identifier that has been dispatched.</param>
        public void MarkDispatched(Int64 id)
        {
            eventStore.MarkDispatched(id);

            statistics.IncrementUpdateCount();
        }

        /// <summary>
        /// Migrates the commit <paramref name="headers"/> and <paramref name="events"/> for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique commit identifier.</param>
        /// <param name="headers">The new commit headers.</param>
        /// <param name="events">The new commit events.</param>
        public void Migrate(Int64 id, HeaderCollection headers, EventCollection events)
        {
            eventStore.Migrate(id, headers, events);

            statistics.IncrementUpdateCount();
        }

        /// <summary>
        /// Deletes all existing commits from the event store.
        /// </summary>
        public void Purge()
        {
            eventStore.Purge();

            statistics.IncrementDeleteCount();
        }
    }
}
