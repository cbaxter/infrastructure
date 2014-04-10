using System;
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
        private readonly Lazy<SagaTimeoutCache> sagaTimeoutCache;
        private readonly Lazy<IPublishEvents> lazyEventPublisher;
        private readonly Object syncLock = new Object();
        private DateTime scheduledTimeout;
        private Boolean disposed;

        /// <summary>
        /// The underlying saga timeout cache instance.
        /// </summary>
        internal SagaTimeoutCache TimeoutCache { get { return sagaTimeoutCache.Value; } }

        /// <summary>
        /// Initializes a new instance of <see cref="TimeoutDispatcher"/>.
        /// </summary>
        /// <param name="sagaStore">The saga store.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        public TimeoutDispatcher(Lazy<IStoreSagas> sagaStore, Lazy<IPublishEvents> eventPublisher)
        {
            Verify.NotNull(sagaStore, "sagaStore");
            Verify.NotNull(eventPublisher, "eventPublisher");

            this.lazyEventPublisher = eventPublisher;
            this.sagaTimeoutCache = new Lazy<SagaTimeoutCache>(() => new SagaTimeoutCache(sagaStore.Value, Settings.SagaStore.TimeoutCacheDuration));
            this.timer = new TimerWrapper(_ => DispatchElapsedTimeouts(), null, System.Threading.Timeout.InfiniteTimeSpan, System.Threading.Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TimeoutDispatcher"/>.
        /// </summary>
        /// <param name="sagaStore">The saga store.</param>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="timer">The timer factory.</param>
        internal TimeoutDispatcher(Lazy<IStoreSagas> sagaStore, Lazy<IPublishEvents> eventPublisher, Func<Action, ITimer> timer)
        {
            Verify.NotNull(timer, "timer");
            Verify.NotNull(sagaStore, "sagaStore");
            Verify.NotNull(eventPublisher, "eventPublisher");

            this.lazyEventPublisher = eventPublisher;
            this.sagaTimeoutCache = new Lazy<SagaTimeoutCache>(() => new SagaTimeoutCache(sagaStore.Value, Settings.SagaStore.TimeoutCacheDuration));
            this.timer = timer(DispatchElapsedTimeouts);
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
        /// Ensure any un-dispatched timeouts are processed before handling new timeouts.
        /// </summary>
        /// <remarks>
        /// The dispatcher must be started after the IoC container has been built to ensure that any <see cref="Lazy{T}"/> circular dependencies can be resolved.
        /// </remarks>
        public void EnsureElapsedTimeoutsDispatched()
        {
            DispatchElapsedTimeouts();
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
                var timeout = saga.Timeout.Value;

                TimeoutCache.ScheduleTimeout(new SagaTimeout(saga.CorrelationId, saga.GetType(), saga.Version, timeout));
            }
            else
            {
                TimeoutCache.ClearTimeout(new SagaReference(saga.GetType(), saga.CorrelationId));
            }

            RescheduleTimer(TimeoutCache.GetNextScheduledTimeout());
        }

        /// <summary>
        /// Publish any pending saga timeouts.
        /// </summary>
        private void DispatchElapsedTimeouts()
        {
            try
            {
                var sagaTimeouts = TimeoutCache.GetElapsedTimeouts();

                DispatchSagaTimeouts(sagaTimeouts);
                RescheduleTimer(TimeoutCache.GetNextScheduledTimeout());
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

            var eventPublisher = lazyEventPublisher.Value;
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
