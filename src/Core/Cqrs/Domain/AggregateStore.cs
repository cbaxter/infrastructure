using System;
using Spark.Configuration;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Eventing;
using Spark.EventStore;
using Spark.Logging;
using Spark.Messaging;
using Spark.Resources;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Spark.Cqrs.Domain
{
    /// <summary>
    /// The primary <see cref="IStoreAggregates"/> implementation that loads/persists aggregate instances to underlying <see cref="IStoreEvents"/> and <see cref="IStoreSnapshots"/> implementations.
    /// </summary>
    public sealed class AggregateStore : IStoreAggregates
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IApplyEvents aggregateUpdater;
        private readonly IStoreSnapshots snapshotStore;
        private readonly IStoreEvents eventStore;
        private readonly Int32 snapshotInterval;

        /// <summary>
        /// Initializes a new instance of <see cref="AggregateStore"/>.
        /// </summary>
        public AggregateStore(IApplyEvents aggregateUpdater, IStoreSnapshots snapshotStore, IStoreEvents eventStore)
            : this(aggregateUpdater, snapshotStore, eventStore, Settings.Default.AggregateStore)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="AggregateStore"/> using the specified <paramref name="settings"/>.
        /// </summary>
        internal AggregateStore(IApplyEvents aggregateUpdater, IStoreSnapshots snapshotStore, IStoreEvents eventStore, IStoreAggregateSettings settings)
        {
            Verify.NotNull(settings, nameof(settings));
            Verify.NotNull(eventStore, nameof(eventStore));
            Verify.NotNull(snapshotStore, nameof(snapshotStore));
            Verify.NotNull(aggregateUpdater, nameof(aggregateUpdater));

            this.snapshotInterval = settings.SnapshotInterval;
            this.aggregateUpdater = aggregateUpdater;
            this.snapshotStore = snapshotStore;
            this.eventStore = eventStore;
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="CachedAggregateStore"/> class.
        /// </summary>
        public void Dispose()
        { }

        /// <summary>
        /// Retrieve the aggregate of the specified <paramref name="aggregateType"/> and aggregate <paramref name="id"/>.
        /// </summary>
        /// <param name="aggregateType">The type of aggregate to retrieve.</param>
        /// <param name="id">The unique aggregate id.</param>
        public Aggregate Get(Type aggregateType, Guid id)
        {
            Verify.NotNull(aggregateType, nameof(aggregateType));
            Verify.TypeDerivesFrom(typeof(Aggregate), aggregateType, nameof(aggregateType));

            var aggregate = GetOrCreate(aggregateType, id);
            var originalAggregateVersion = aggregate.Version;
            var commits = eventStore.GetStream(id, aggregate.Version + 1);

            foreach (var commit in commits)
                ApplyCommitToAggregate(commit, aggregate);

            if (aggregate.Version - originalAggregateVersion >= snapshotInterval)
                snapshotStore.Save(new Snapshot(aggregate.Id, aggregate.Version, aggregate));

            return aggregate;
        }

        /// <summary>
        /// Gets or creates the specified <paramref name="aggregateType"/> identified by the provided aggregate <paramref name="id"/>.
        /// </summary>
        /// <param name="aggregateType">The type of aggregate to retrieve.</param>
        /// <param name="id">The unique aggregate id.</param>
        private Aggregate GetOrCreate(Type aggregateType, Guid id)
        {
            Snapshot snapshot = snapshotStore.GetLastSnapshot(aggregateType, id);
            Aggregate aggregate;

            if (snapshot == null)
            {
                aggregate = AggregateActivator.CreateInstance(aggregateType, id, 0);
            }
            else
            {
                aggregate = (Aggregate)snapshot.State;
                aggregate.Version = snapshot.Version;
                aggregate.Id = id;
            }

            return aggregate;
        }

        /// <summary>
        /// Save the specified <paramref name="context"/> changes for the given aggregate.
        /// </summary>
        /// <param name="aggregate">The current aggregate version for which the context applies.</param>
        /// <param name="context">The command context containing the aggregate changes to be applied.</param>
        public SaveResult Save(Aggregate aggregate, CommandContext context)
        {
            Verify.NotNull(aggregate, nameof(aggregate));
            Verify.NotNull(context, nameof(context));

            var commit = CreateCommit(aggregate, context);
            try
            {
                eventStore.Save(commit);
            }
            catch (DuplicateCommitException)
            {
                Log.WarnFormat("Duplicate commit: {0}", commit);
            }

            // NOTE: Apply commit directly to existing aggregate. By default, each call to `Get` returns a new `Aggregate` instance.
            //       Should the caller hold on to a reference to `this` aggregate instance, it is their responsibility to gaurd against
            //       modifications (via `aggregate.Copy()` call) if they require an unaltered instance of `aggregate`.
            ApplyCommitToAggregate(commit, aggregate);
            if (aggregate.Version > 0 && aggregate.Version % snapshotInterval == 0)
                snapshotStore.Save(new Snapshot(aggregate.Id, aggregate.Version, aggregate));

            return new SaveResult(aggregate, commit);
        }

        /// <summary>
        /// Creates a commit for the specified <paramref name="aggregate"/> based on the provided <paramref name="context"/>.
        /// </summary>
        /// <param name="aggregate">The <see cref="Aggregate"/> instance for which the commit is to be applied.</param>
        /// <param name="context">The <see cref="CommandContext"/> instance containing the pending modifications to the associated <paramref name="aggregate"/>.</param>
        private static Commit CreateCommit(Aggregate aggregate, CommandContext context)
        {
            EventCollection events = context.GetRaisedEvents();
            HeaderCollection headers;

            if (aggregate.Version == 0)
            {
                var typeHeader = new Header(Header.Aggregate, aggregate.GetType().GetFullNameWithAssembly(), checkReservedNames: false);

                headers = new HeaderCollection(context.Headers.Concat(typeHeader));
            }
            else
            {
                headers = context.Headers;
            }

            return new Commit(context.CommandId, aggregate.Id, aggregate.Version + 1, headers, events);
        }

        /// <summary>
        /// Applies all <see cref="Commit.Events"/> to the specified <paramref name="aggregate"/> instance.
        /// </summary>
        /// <param name="commit">The <see cref="Commit"/> to be applied to the specified <paramref name="aggregate"/>.</param>
        /// <param name="aggregate">The <see cref="Aggregate"/> instance for which the commit is to be applied.</param>
        private void ApplyCommitToAggregate(Commit commit, Aggregate aggregate)
        {
            var expectedVersion = aggregate.Version + 1;
            if (commit.Version != expectedVersion)
                throw new InvalidOperationException(Exceptions.MissingAggregateCommits.FormatWith(expectedVersion, commit.Version));

            aggregate.Version = commit.Version;
            foreach (var e in commit.Events)
            {
                using (new EventContext(aggregate.Id, commit.Headers, e))
                    aggregateUpdater.Apply(e, aggregate);
            }
        }
    }
}
