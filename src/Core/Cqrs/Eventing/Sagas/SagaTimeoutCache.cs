using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// Represents a thread-safe in-memory cache of pending saga timeouts.
    /// </summary>
    internal sealed class SagaTimeoutCache
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private static readonly TimeSpan MinimumCacheDuration = TimeSpan.FromMinutes(5);
        private readonly SagaTimeoutCollection sagaTimeouts = new SagaTimeoutCollection();
        private readonly Object syncLock = new Object();
        private readonly TimeSpan timeoutCacheDuration;
        private readonly IStoreSagas sagaStore;
        private DateTime maximumCachedTimeout;

        /// <summary>
        /// Get the current number of saga timeouts cached in this <see cref="SagaTimeoutCache"/> instance.
        /// </summary>
        public Int32 Count { get { lock (syncLock) { return sagaTimeouts.Count; } } }

        /// <summary>
        /// Initializes a new instance of <see cref="SagaTimeoutCache"/>.
        /// </summary>
        /// <param name="sagaStore">The saga store used to retrieve pending saga timeouts.</param>
        /// <param name="timeoutCacheDuration">The maximum cache duration for a given saga timeout (5 minute minimum).</param>
        public SagaTimeoutCache(IStoreSagas sagaStore, TimeSpan timeoutCacheDuration)
        {
            Verify.NotNull(sagaStore, nameof(sagaStore));

            this.sagaStore = sagaStore;
            this.maximumCachedTimeout = DateTime.MinValue;
            this.timeoutCacheDuration = timeoutCacheDuration < MinimumCacheDuration ? MinimumCacheDuration : timeoutCacheDuration;
        }

        /// <summary>
        /// Get the next scheduled saga timeout or the maximum cacchable timeout (thread-safe).
        /// </summary>
        public DateTime GetNextScheduledTimeout()
        {
            lock (syncLock)
            {
                return sagaTimeouts.Count > 0 ? sagaTimeouts.First().Timeout : maximumCachedTimeout;
            }
        }

        /// <summary>
        /// Get the set of elapsed saga timeouts (thread-safe).
        /// </summary>
        public IEnumerable<SagaTimeout> GetElapsedTimeouts()
        {
            Log.Trace("Getting elapsed saga timeouts");

            lock (syncLock)
            {
                CacheScheduleTimeoutsIfRequired();

                return GetAndClearElapsedSagaTimeouts();
            }
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

            Log.Debug("Loading saga timeouts scheduled before {0} from data store", maximumCachedTimeout);

            foreach (var sagaTimeout in sagaStore.GetScheduledTimeouts(maximumCachedTimeout))
                ScheduleTimeoutInternal(sagaTimeout);

            Log.Trace("Loaded {0} saga timeouts", sagaTimeouts.Count);
        }

        /// <summary>
        /// Get the set of saga timeouts that have elapsed.
        /// </summary>
        private IEnumerable<SagaTimeout> GetAndClearElapsedSagaTimeouts()
        {
            if (sagaTimeouts.Count == 0) return Enumerable.Empty<SagaTimeout>();

            var elapsedTimeouts = GetElapsedSagaTimeouts().ToList();
            foreach (var elapsedTimeout in elapsedTimeouts)
                ClearTimeoutInternal(elapsedTimeout);

            return elapsedTimeouts;
        }

        /// <summary>
        /// Get the set of elapsed saga timeouts.
        /// </summary>
        private IEnumerable<SagaTimeout> GetElapsedSagaTimeouts()
        {
            var now = SystemTime.Now;
            foreach (var sagaTimeout in sagaTimeouts)
            {
                if (sagaTimeout.Timeout <= now)
                    yield return sagaTimeout;
                else
                    yield break;
            }
        }

        /// <summary>
        /// Schedule the specified <paramref name="sagaTimeout"/> (thread-safe).
        /// </summary>
        /// <param name="sagaTimeout">The saga timeout to schedule.</param>
        public void ScheduleTimeout(SagaTimeout sagaTimeout)
        {
            Log.Trace("Scheduling saga timeout for {0}", sagaTimeout);

            lock (syncLock)
            {
                ScheduleTimeoutInternal(sagaTimeout);
            }

            Log.Trace("Saga timeout scheduled for {0}", sagaTimeout);
        }

        /// <summary>
        /// Schedule the specified <paramref name="sagaTimeout"/>.
        /// </summary>
        /// <param name="sagaTimeout">The saga timeout to schedule.</param>
        private void ScheduleTimeoutInternal(SagaTimeout sagaTimeout)
        {
            if (sagaTimeout.Timeout < maximumCachedTimeout)
                sagaTimeouts.Add(sagaTimeout);
        }

        /// <summary>
        /// Clear the saga timeout associated with the specified <paramref name="sagaReference"/> (thread-safe).
        /// </summary>
        /// <param name="sagaReference">The saga reference to clear a scheduled timeout.</param>
        public void ClearTimeout(SagaReference sagaReference)
        {
            Log.Trace("Clearing saga timeout for {0}", sagaReference);

            lock (syncLock)
            {
                ClearTimeoutInternal(sagaReference);
            }

            Log.Trace("Saga timeout cleared for {0}", sagaReference);
        }

        /// <summary>
        /// Clear the saga timeout associated with the specified <paramref name="sagaReference"/>.
        /// </summary>
        /// <param name="sagaReference">The saga reference to clear a scheduled timeout.</param>
        private void ClearTimeoutInternal(SagaReference sagaReference)
        {
            sagaTimeouts.Remove(sagaReference);
        }

        /// <summary>
        /// Clear the currently cached saga timeout values (thread safe).
        /// </summary>
        public void Clear()
        {
            lock (syncLock)
            {
                maximumCachedTimeout = DateTime.MinValue;
                sagaTimeouts.Clear();
            }
        }
    }
}
