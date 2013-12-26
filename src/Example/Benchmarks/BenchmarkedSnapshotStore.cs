using System;
using Spark.EventStore;

namespace Spark.Example.Benchmarks
{
    /// <summary>
    /// A snapshot store decorator class to capture read/write operations.
    /// </summary>
    internal sealed class BenchmarkedSnapshotStore : IStoreSnapshots
    {
        private readonly Statistics statistics;
        private readonly IStoreSnapshots snapshotStore;

        /// <summary>
        /// Initalizes a new isntance of <see cref="BenchmarkedSnapshotStore"/>.
        /// </summary>
        /// <param name="snapshotStore">The snapshot store to decorate.</param>
        /// <param name="statistics">The statistics class.</param>
        public BenchmarkedSnapshotStore(IStoreSnapshots snapshotStore, Statistics statistics)
        {
            this.snapshotStore = snapshotStore;
            this.statistics = statistics;
        }

        /// <summary>
        /// Gets the most recent snapshot for the specified <paramref name="streamId"/> and <paramref name="maximumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="maximumVersion">The maximum snapshot version.</param>
        public Snapshot GetSnapshot(Guid streamId, Int32 maximumVersion)
        {
            var result = snapshotStore.GetSnapshot(streamId, maximumVersion);

            statistics.IncrementQueryCount();

            return result;
        }

        /// <summary>
        /// Adds a new snapshot to the snapshot store, keeping all existing snapshots.
        /// </summary>
        /// <param name="snapshot">The snapshot to append to the snapshot store.</param>
        public void Save(Snapshot snapshot)
        {
            snapshotStore.Save(snapshot);

            if (snapshot.Version == 1)
                statistics.IncrementInsertCount();
            else
                statistics.IncrementUpdateCount();
        }

        /// <summary>
        /// Deletes all existing snapshots from the snapshot store.
        /// </summary>
        public void Purge()
        {
            snapshotStore.Purge();

            statistics.IncrementDeleteCount();
        }
    }
}
