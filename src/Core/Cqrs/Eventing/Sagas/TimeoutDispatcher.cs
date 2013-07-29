using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Spark.Configuration;
using Spark.Messaging;

/* Copyright (c) 2013 Spark Software Ltd.
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

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// A <see cref="HookableSagaStore"/> pipeline hook used to dispatch saga timeout events after successfully commited to the underlying data store.
    /// </summary>
    public sealed class TimeoutDispatcher : PipelineHook
    {
        private readonly Timer timer;
        private readonly IStoreSagas sagaStore;
        private readonly IPublishEvents eventPublisher;
        private readonly Object syncLock = new Object();
        private readonly SortedDictionary<DateTime, HashSet<SagaReference>> sortedSagaTimeouts = new SortedDictionary<DateTime, HashSet<SagaReference>>();
        private readonly Dictionary<SagaReference, SagaTimeout> scheduledSagaTimeouts = new Dictionary<SagaReference, SagaTimeout>();
        private readonly TimeSpan timeoutCacheDuration;
        private DateTime maximumCachedTimeout;
        private Boolean suppressReschedule;
        private Boolean disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="TimeoutDispatcher"/>.
        /// </summary>
        /// <param name="sagaStore">The saga store.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        public TimeoutDispatcher(IStoreSagas sagaStore, IPublishEvents eventPublisher)
            : this(sagaStore, eventPublisher, Settings.SagaStore)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="TimeoutDispatcher"/>.
        /// </summary>
        /// <param name="sagaStore">The saga store.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="settings">The saga store settings.</param>
        internal TimeoutDispatcher(IStoreSagas sagaStore, IPublishEvents eventPublisher, IStoreSagaSettings settings)
        {
            Verify.NotNull(sagaStore, "sagaStore");
            Verify.NotNull(eventPublisher, "eventPublisher");

            this.sagaStore = sagaStore;
            this.eventPublisher = eventPublisher;
            this.timeoutCacheDuration = settings.TimeoutCacheDuration;
            this.timer = new Timer(_ => OnTimerElapsed(), null, 0, System.Threading.Timeout.Infinite);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="PipelineHook"/> class.
        /// </summary>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            if (!disposing || disposed)
                return;

            disposed = true;
            lock (syncLock)
            {
                timer.Dispose();
            }
        }

        /// <summary>
        ///  When overriden, defines the custom behavior to be invoked after attempting to save the specified <paramref name="saga"/>.
        /// </summary>
        /// <param name="saga">The modified <see cref="Saga"/> instance if <paramref name="error"/> is <value>null</value>; otherwise the original <see cref="Saga"/> instance if <paramref name="error"/> is not <value>null</value>.</param>
        /// <param name="context">The current <see cref="SagaContext"/> assocaited with the pending saga modifications.</param>
        /// <param name="error">The <see cref="Exception"/> thrown if the save was unsuccessful; otherwise <value>null</value>.</param>
        public override void PostSave(Saga saga, SagaContext context, Exception error)
        {
            if (saga == null || error != null)
                return;

            lock (syncLock)
            {
                if (saga.Timeout.HasValue && !saga.Completed)
                {
                    ScheduleTimeout(new SagaTimeout(saga.CorrelationId, saga.GetType(), saga.Version, saga.Timeout.Value));
                }
                else
                {
                    ClearTimeout(new SagaReference(saga.GetType(), saga.CorrelationId));
                }

                if (!suppressReschedule)
                    RescheduleTimer();
            }
        }

        /// <summary>
        /// Publish any pending saga timeouts.
        /// </summary>
        private void OnTimerElapsed()
        {
            IEnumerable<SagaTimeout> sagaTimeouts;

            lock (syncLock)
            {
                suppressReschedule = true;
                CacheScheduleTimeoutsIfRequired();
                sagaTimeouts = GetScheduledTimeoutsToDispatch();
            }

            DispatchSagaTimeouts(sagaTimeouts);

            lock (syncLock)
            {
                RescheduleTimer();
                suppressReschedule = false;
            }
        }

        /// <summary>
        /// Update the next timer callback based on the time of the next saga timeout.
        /// </summary>
        private void RescheduleTimer()
        {
            const Int64 minimumInterval = 100;
            const Int64 maximumInterval = 60000;
            var nextTimeout = scheduledSagaTimeouts.Count > 0 ? sortedSagaTimeouts.First().Key : maximumCachedTimeout;
            var dueTime = Math.Min(maximumInterval, Math.Max(nextTimeout.Subtract(SystemTime.Now).Ticks / TimeSpan.TicksPerMillisecond, minimumInterval));

            if(!disposed)
                timer.Change(dueTime, System.Threading.Timeout.Infinite);
        }

        /// <summary>
        /// Get any scheduled saga timeouts from the underlying data store if required.
        /// </summary>
        private void CacheScheduleTimeoutsIfRequired()
        {
            var now = SystemTime.Now;
            if (maximumCachedTimeout >= now)
                return;

            maximumCachedTimeout = now.Add(timeoutCacheDuration);
            foreach (var sagaTimeout in sagaStore.GetScheduledTimeouts(maximumCachedTimeout))
                ScheduleTimeout(sagaTimeout);
        }

        /// <summary>
        /// Get the set of saga timeouts to dispatch.
        /// </summary>
        private IEnumerable<SagaTimeout> GetScheduledTimeoutsToDispatch()
        {
            if (sortedSagaTimeouts.Count == 0)
                return Enumerable.Empty<SagaTimeout>();

            var now = SystemTime.Now;
            var sagaReferences = sortedSagaTimeouts.Where(item => item.Key <= now).SelectMany(item => item.Value).ToArray();
            if (sagaReferences.Length == 0)
                return Enumerable.Empty<SagaTimeout>();

            var sagaTimeouts = new List<SagaTimeout>(sagaReferences.Length);
            foreach (var sagaReference in sagaReferences)
            {
                SagaTimeout sagaTimeout;
                if (!scheduledSagaTimeouts.TryGetValue(sagaReference, out sagaTimeout))
                    continue;

                sagaTimeouts.Add(sagaTimeout);

                ClearTimeout(sagaReference);
            }

            return sagaTimeouts;
        }

        /// <summary>
        /// Publish any pending saga timeouts.
        /// </summary>
        /// <param name="sagaTimeouts">The set of saga timeouts to dispatch.</param>
        private void DispatchSagaTimeouts(IEnumerable<SagaTimeout> sagaTimeouts)
        {
            foreach (var sagaTimeout in sagaTimeouts)
            {
                var eventVersion = new EventVersion(sagaTimeout.Version, 1, 1);
                var e = new Timeout(sagaTimeout.SagaType, sagaTimeout.Timeout);

                eventPublisher.Publish(HeaderCollection.Empty, new EventEnvelope(GuidStrategy.NewGuid(), sagaTimeout.SagaId, eventVersion, e));
            }
        }

        /// <summary>
        /// Schedule the specified <paramref name="sagaTimeout"/>.
        /// </summary>
        /// <param name="sagaTimeout">The saga timeout to schedule.</param>
        private void ScheduleTimeout(SagaTimeout sagaTimeout)
        {
            var sagaReference = new SagaReference(sagaTimeout.SagaType, sagaTimeout.SagaId);
            var timeout = sagaTimeout.Timeout;

            if (timeout >= maximumCachedTimeout)
                return;

            HashSet<SagaReference> sagaReferences;
            if (!sortedSagaTimeouts.TryGetValue(timeout, out sagaReferences))
                sortedSagaTimeouts.Add(timeout, sagaReferences = new HashSet<SagaReference>());

            sagaReferences.Add(sagaReference);
            scheduledSagaTimeouts[sagaReference] = sagaTimeout;
        }

        /// <summary>
        /// Clear the saga timeout associated with the specified <paramref name="sagaReference"/>.
        /// </summary>
        /// <param name="sagaReference"></param>
        private void ClearTimeout(SagaReference sagaReference)
        {
            SagaTimeout sagaTimeout;
            if (!scheduledSagaTimeouts.TryGetValue(sagaReference, out sagaTimeout))
                return;

            scheduledSagaTimeouts.Remove(sagaReference);

            HashSet<SagaReference> sagaReferences;
            if (!sortedSagaTimeouts.TryGetValue(sagaTimeout.Timeout, out sagaReferences))
                return;

            sagaReferences.Remove(sagaReference);
            if (sagaReferences.Count == 0)
                sortedSagaTimeouts.Remove(sagaTimeout.Timeout);
        }
    }
}
