﻿using System;
using System.Collections.Generic;
using Spark.Configuration;
using Spark.Logging;
using Spark.Messaging;
using Spark.Threading;

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
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly ITimer timer;
        private readonly IPublishEvents eventPublisher;
        private readonly SagaTimeoutCache sagaTimeoutCache;
        private readonly Object syncLock = new Object();
        private DateTime scheduledTimeout;
        private Boolean disposed;

        /// <summary>
        /// The underlying saga timeout cache instance.
        /// </summary>
        internal SagaTimeoutCache Cache { get { return sagaTimeoutCache; } }

        /// <summary>
        /// Initializes a new instance of <see cref="TimeoutDispatcher"/>.
        /// </summary>
        /// <param name="sagaStore">The saga store.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        public TimeoutDispatcher(IStoreSagas sagaStore, IPublishEvents eventPublisher)
        {
            Verify.NotNull(sagaStore, "sagaStore");
            Verify.NotNull(eventPublisher, "eventPublisher");

            this.eventPublisher = eventPublisher;
            this.sagaTimeoutCache = new SagaTimeoutCache(sagaStore, Settings.SagaStore.TimeoutCacheDuration);
            this.timer = new TimerWrapper(_ => OnTimerElapsed(), null, TimeSpan.Zero, System.Threading.Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TimeoutDispatcher"/>.
        /// </summary>
        /// <param name="sagaStore">The saga store.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="timer">The timer factory.</param>
        internal TimeoutDispatcher(IStoreSagas sagaStore, IPublishEvents eventPublisher, Func<Action, ITimer> timer)
        {
            Verify.NotNull(timer, "timer");
            Verify.NotNull(sagaStore, "sagaStore");
            Verify.NotNull(eventPublisher, "eventPublisher");

            this.eventPublisher = eventPublisher;
            this.sagaTimeoutCache = new SagaTimeoutCache(sagaStore, Settings.SagaStore.TimeoutCacheDuration);
            this.timer = timer(OnTimerElapsed);
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
            if (!context.TimeoutChanged || saga == null || error != null)
                return;

            if (saga.Timeout.HasValue && !saga.Completed)
            {
                sagaTimeoutCache.ScheduleTimeout(new SagaTimeout(saga.CorrelationId, saga.GetType(), saga.Version, saga.Timeout.Value));
            }
            else
            {
                sagaTimeoutCache.ClearTimeout(new SagaReference(saga.GetType(), saga.CorrelationId));
            }

            RescheduleTimer(sagaTimeoutCache.GetNextScheduledTimeout());
        }

        /// <summary>
        /// Publish any pending saga timeouts.
        /// </summary>
        private void OnTimerElapsed()
        {
            try
            {
                var sagaTimeouts = sagaTimeoutCache.GetElapsedTimeouts();

                DispatchSagaTimeouts(sagaTimeouts);
                RescheduleTimer(sagaTimeoutCache.GetNextScheduledTimeout());
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                RescheduleTimer(SystemTime.Now.AddSeconds(10));
            }
        }

        /// <summary>
        /// Update the next timer callback based on the time of the next saga timeout.
        /// </summary>
        private void RescheduleTimer(DateTime timeout)
        {
            const Int64 minimumInterval = 100;
            const Int64 maximumInterval = 60000;

            lock (syncLock)
            {
                if (timeout == scheduledTimeout || disposed)
                    return;

                var dueTime = Math.Min(maximumInterval, Math.Max(timeout.Subtract(SystemTime.Now).Ticks / TimeSpan.TicksPerMillisecond, minimumInterval));

                scheduledTimeout = timeout;
                timer.Change(dueTime, System.Threading.Timeout.Infinite);

                Log.TraceFormat("Timer due in {0}ms", dueTime);
            }
        }

        /// <summary>
        /// Publish any pending saga timeouts.
        /// </summary>
        /// <param name="sagaTimeouts">The set of saga timeouts to dispatch.</param>
        private void DispatchSagaTimeouts(IEnumerable<SagaTimeout> sagaTimeouts)
        {
            Log.Trace("Dispatching saga timeouts");

            foreach (var sagaTimeout in sagaTimeouts)
            {
                var eventVersion = new EventVersion(sagaTimeout.Version, 1, 1);
                var e = new Timeout(sagaTimeout.SagaType, sagaTimeout.Timeout);

                eventPublisher.Publish(HeaderCollection.Empty, new EventEnvelope(GuidStrategy.NewGuid(), sagaTimeout.SagaId, eventVersion, e));
            }

            Log.Trace("Saga timeouts dispatched");
        }
    }
}
