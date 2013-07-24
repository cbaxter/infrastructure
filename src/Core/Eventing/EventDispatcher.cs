using System;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Logging;

/* Copyright (c) 2012 Spark Software Ltd.
 * 
 * This source is subject to the GNU Lesser General Public License.
 * See: http://www.gnu.org/copyleft/lesser.html
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE. 
 */

namespace Spark.Infrastructure.Eventing
{
    /// <summary>
    /// An <see cref="HookableAggregateStore"/> pipeline hook used to dispatch events after successfully commited to the underlying data store.
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
            Verify.NotNull(eventStore, "eventStore");
            Verify.NotNull(eventPublisher, "eventPublisher");

            this.eventStore = eventStore;
            this.eventPublisher = eventPublisher;
            this.markDispatched = settings.MarkDispatched;

            EnsurePersistedCommitsDispatched();
        }

        /// <summary>
        /// Ensure any un-dispatched commits are processed before handling new commits.
        /// </summary>
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

        /// <summary>
        ///  When overriden, defines the custom behavior to be invoked after attempting to save the specified <paramref name="aggregate"/>.
        /// </summary>
        /// <param name="aggregate">The modified <see cref="Aggregate"/> instance if <paramref name="commit"/> is not <value>null</value>; otherwise the original <see cref="Aggregate"/> instance if <paramref name="error"/> is not <value>null</value>.</param>
        /// <param name="commit">The <see cref="Commit"/> generated if the save was successful; otherwise <value>null</value>.</param>
        /// <param name="error">The <see cref="Exception"/> thrown if the save was unsuccessful; otherwise <value>null</value>.</param>
        public override void PostSave(Aggregate aggregate, Commit commit, Exception error)
        {
            if (commit != null && commit.Id.HasValue)
                DispatchCommit(commit);
            else
                Log.WarnFormat("Commit not dispatched");
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
