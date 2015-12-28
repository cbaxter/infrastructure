using System;
using Spark.Configuration;
using Spark.Cqrs.Domain;
using Spark.EventStore;
using Spark.Logging;

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

namespace Spark.Cqrs.Eventing
{
    /// <summary>
    /// A <see cref="HookableAggregateStore"/> pipeline hook used to dispatch events after successfully commited to the underlying data store.
    /// </summary>
    public sealed class EventDispatcher : PipelineHook
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IPublishEvents eventPublisher;
        private readonly IStoreEvents eventStore;
        private readonly Boolean markDispatched;

        /// <summary>
        /// Initializes a new instance of <see cref="EventDispatcher"/>.
        /// </summary>
        /// <param name="eventStore">The event store.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        public EventDispatcher(IStoreEvents eventStore, IPublishEvents eventPublisher)
            : this(eventStore, eventPublisher, Settings.EventStore)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="EventDispatcher"/>.
        /// </summary>
        /// <param name="eventStore">The event store.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="settings">The current event store settings.</param>
        internal EventDispatcher(IStoreEvents eventStore, IPublishEvents eventPublisher, IStoreEventSettings settings)
        {
            Verify.NotNull(eventStore, nameof(eventStore));
            Verify.NotNull(eventPublisher, nameof(eventPublisher));

            this.eventStore = eventStore;
            this.eventPublisher = eventPublisher;
            this.markDispatched = settings.MarkDispatched;
        }

        /// <summary>
        /// Ensure any un-dispatched commits are processed before handling new commits.
        /// </summary>
        /// <remarks>
        /// The dispatcher must be started after the IoC container has been built to ensure that any <see cref="Lazy{T}"/> circular dependencies can be resolved.
        /// </remarks>
        public void EnsurePersistedCommitsDispatched()
        {
            if (!markDispatched)
                return;

            foreach (var commit in eventStore.GetUndispatched())
            {
                Log.Warn("Processing undispatched commit {0}", commit.Id);
                DispatchCommit(commit);
            }
        }

        /// <summary>
        ///  When overriden, defines the custom behavior to be invoked after attempting to save the specified <paramref name="aggregate"/>.
        /// </summary>
        /// <param name="aggregate">The modified <see cref="Aggregate"/> instance if <paramref name="commit"/> is not <value>null</value>; otherwise the original <see cref="Aggregate"/> instance if <paramref name="error"/> is not <value>null</value>.</param>
        /// <param name="commit">The <see cref="Commit"/> generated if the save was successful; otherwise <value>null</value>.</param>
        /// <param name="error">The <see cref="Exception"/> thrown if the save was unsuccessful; otherwise <value>null</value>.</param>
        public override void PostSave(Aggregate aggregate, Commit commit, Exception error)
        {
            if (commit?.Id != null)
                DispatchCommit(commit);
            else
                Log.Warn("Commit not dispatched");
        }

        /// <summary>
        /// Dispatch all events within the specified <paramref name="commit"/>.
        /// </summary>
        /// <param name="commit">The commit instance to be dispatched.</param>
        private void DispatchCommit(Commit commit)
        {
            var commitId = commit.Id.GetValueOrDefault();
            var events = commit.Events;

            for (var i = 0; i < events.Count; i++)
            {
                var e = events[i];
                var version = new EventVersion(commit.Version, events.Count, i + 1);

                eventPublisher.Publish(commit.Headers, new EventEnvelope(commit.CorrelationId, commit.StreamId, version, e));
            }

            if (markDispatched)
                eventStore.MarkDispatched(commitId);
        }
    }
}
