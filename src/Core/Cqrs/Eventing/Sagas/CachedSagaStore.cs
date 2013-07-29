using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using Spark.Configuration;
using Spark.Data;
using Spark.Logging;

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
    /// A <see cref="IStoreSagas"/> wrapper class to enable <see cref="MemoryCache"/> storage of <see cref="Saga"/> instances to reduce <see cref="TryGetSaga"/> overhead.
    /// </summary>
    public sealed class CachedSagaStore : IStoreSagas
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly TimeSpan slidingExpiration;
        private readonly MemoryCache memoryCache;
        private readonly IStoreSagas sagaStore;

        /// <summary>
        /// Initializes a new instance of <see cref="CachedSagaStore"/>.
        /// </summary>
        /// <param name="sagaStore">The underlying <see cref="IStoreSagas"/> implementation to be decorated.</param>
        public CachedSagaStore(IStoreSagas sagaStore)
            : this(sagaStore, Settings.SagaStore.CacheSlidingExpiration, new MemoryCache("SagaCache"))
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="CachedSagaStore"/> using the specified <paramref name="slidingExpiration"/>.
        /// </summary>
        /// <param name="sagaStore">The underlying <see cref="IStoreSagas"/> implementation to be decorated.</param>
        /// <param name="slidingExpiration">The maximum time an <see cref="Saga"/> may existing in the cache without being accessed.</param>
        /// <param name="memoryCache">The underlying cache implementation.</param>
        internal CachedSagaStore(IStoreSagas sagaStore, TimeSpan slidingExpiration, MemoryCache memoryCache)
        {
            Verify.NotNull(sagaStore, "sagaStore");
            Verify.NotNull(memoryCache, "memoryCache");
            Verify.GreaterThanOrEqual(TimeSpan.FromSeconds(1), slidingExpiration, "sagaStore");

            this.sagaStore = sagaStore;
            this.memoryCache = memoryCache;
            this.slidingExpiration = slidingExpiration;
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="CachedSagaStore"/> class.
        /// </summary>
        public void Dispose()
        {
            memoryCache.Dispose();
            sagaStore.Dispose();
        }

        /// <summary>
        /// Creates a new saga instance identified by the specified <paramref name="type"/> and <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The type of saga to be retrieved.</param>
        /// <param name="id">The correlation id of the saga to be retrieved.</param>
        public Saga CreateSaga(Type type, Guid id)
        {
            return sagaStore.CreateSaga(type, id);
        }

        /// <summary>
        /// Attempt to retrieve an existing saga instance identified by the specified <paramref name="type"/> and <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The type of saga to be retrieved.</param>
        /// <param name="id">The correlation id of the saga to be retrieved.</param>
        /// <param name="saga">The <see cref="Saga"/> instance if found; otherwise <value>null</value>.</param>
        public Boolean TryGetSaga(Type type, Guid id, out Saga saga)
        {
            Verify.NotNull(type, "type");
            
            // Since we do not want to cache NULL values, we will simply use Get to check if the desired
            // saga has already been cached; if not then we will call down to our delegate saga store to
            // retrieve the desired saga instance and cache if the requested saga is found.
            var key = String.Concat(type.FullName, "-", id.ToString());

            saga = memoryCache.Get(key) as Saga;
            if (saga == null && sagaStore.TryGetSaga(type, id, out saga))
            {
                Log.TraceFormat("Saga {0}-{1} not found in cache.", type, id);

                memoryCache.Set(key, saga, CreateCacheItemPolicy());
            }

            return saga != null;
        }

        /// <summary>
        /// Get all scheduled saga timeouts before the specified maximum timeout.
        /// </summary>
        /// <param name="maximumTimeout">The exclusive timeout upper bound.</param>
        public IReadOnlyList<SagaTimeout> GetScheduledTimeouts(DateTime maximumTimeout)
        {
            return sagaStore.GetScheduledTimeouts(maximumTimeout);
        }

        /// <summary>
        /// Save the specified <paramref name="context"/> changes for the given <paramref name="saga"/>.
        /// </summary>
        /// <param name="saga">The current saga version for which the context applies.</param>
        /// <param name="context">The saga context containing the saga changes to be applied.</param>
        public Saga Save(Saga saga, SagaContext context)
        {
            Verify.NotNull(saga, "saga");
            Verify.NotNull(context, "context");

            var key = String.Concat(saga.GetType().FullName, "-", saga.CorrelationId.ToString());
            var copy = saga.Copy();

            try
            {
                sagaStore.Save(copy, context);

                if (saga.Completed)
                    memoryCache.Remove(key);
                else
                    memoryCache.Set(key, copy, CreateCacheItemPolicy());

                return copy;
            }
            catch (ConcurrencyException)
            {
                // NOTE: Under a single node configuration, this should not happen as events against a specific
                //       saga are serialized. If a concurrency exception is thrown, then the conflict is likely 
                //       at the persistence level and thus any cached instance of the saga should be purged and
                //       re-cached on subsequent `TryGetSaga` call.
                memoryCache.Remove(key);
                throw;
            }
        }

        /// <summary>
        /// Deletes all existing sagas from the saga store.
        /// </summary>
        public void Purge()
        {
            sagaStore.Purge();
            memoryCache.Trim(100);
        }

        /// <summary>
        /// Creates a sliding expiration cache item policy with an explicit <see cref="CacheItemPolicy.RemovedCallback"/> specified.
        /// </summary>
        /// <returns></returns>
        private CacheItemPolicy CreateCacheItemPolicy()
        {
            var cacheItemPolicy = new CacheItemPolicy { SlidingExpiration = slidingExpiration, RemovedCallback = OnCacheItemRemoved };

            return cacheItemPolicy;
        }

        /// <summary>
        /// Responds to cache items being removed from the underlying <see cref="MemoryCache"/>.
        /// </summary>
        /// <param name="e">Provides information about a cache entry that was removed from the cache.</param>
        private static void OnCacheItemRemoved(CacheEntryRemovedArguments e)
        {
            Log.TraceFormat("Saga {0} was removed: {1}.", e.CacheItem.Key, e.RemovedReason);
        }
    }
}
