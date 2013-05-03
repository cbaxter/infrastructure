using System;
using System.Runtime.Caching;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Configuration;
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

namespace Spark.Infrastructure.Domain
{
    /// <summary>
    /// A <see cref="IStoreAggregates"/> wrapper class to enable <see cref="MemoryCache"/> storage of <see cref="Aggregate"/> instances to reduce <see cref="Get"/> overhead.
    /// </summary>
    public sealed class CachedAggregateStore : IStoreAggregates, IDisposable //TODO: make all stores disposable (and piplineHooks)?
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IStoreAggregates aggregateStore;
        private readonly TimeSpan slidingExpiration;
        private readonly MemoryCache memoryCache;
        private Boolean disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="CachedAggregateStore"/>.
        /// </summary>
        /// <param name="aggregateStore">The underlying <see cref="IStoreAggregates"/> implementation to be decorated.</param>
        public CachedAggregateStore(IStoreAggregates aggregateStore)
            : this(aggregateStore, Settings.AggregateStore.CacheSlidingExpiration)
        {
            Verify.NotNull(aggregateStore, "aggregateStore");

            this.aggregateStore = aggregateStore;
            this.memoryCache = new MemoryCache("AggregateCache");
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CachedAggregateStore"/> using the specified <paramref name="slidingExpiration"/>.
        /// </summary>
        /// <param name="aggregateStore">The underlying <see cref="IStoreAggregates"/> implementation to be decorated.</param>
        /// <param name="slidingExpiration">The maximum time an <see cref="Aggregate"/> may existing in the cache without being accessed.</param>
        internal CachedAggregateStore(IStoreAggregates aggregateStore, TimeSpan slidingExpiration)
        {
            Verify.NotNull(aggregateStore, "aggregateStore");
            Verify.GreaterThanOrEqual(TimeSpan.FromSeconds(1), slidingExpiration, "aggregateStore");

            this.aggregateStore = aggregateStore;
            this.slidingExpiration = slidingExpiration;
            this.memoryCache = new MemoryCache("AggregateCache");
        }

        /// <summary>
        /// Releases all unmanaged resources used by the current instance of the <see cref="CachedAggregateStore"/> class.
        /// </summary>
        ~CachedAggregateStore()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="CachedAggregateStore"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="CachedAggregateStore"/> class.
        /// </summary>
        private void Dispose(Boolean disposing)
        {
            if (!disposing || disposed)
                return;

            memoryCache.Dispose();
            disposed = true;
        }

        /// <summary>
        /// Retrieve the aggregate of the specified <paramref name="aggregateType"/> and aggregate <paramref name="id"/>.
        /// </summary>
        /// <param name="aggregateType">The type of aggregate to retrieve.</param>
        /// <param name="id">The unique aggregate id.</param>
        public Aggregate Get(Type aggregateType, Guid id)
        {
            Verify.NotNull(aggregateType, "aggregateType");

            var key = String.Concat(aggregateType.FullName, "-", id.ToString());
            var lazyValue = new Lazy<Aggregate>(() => LoadAggregate(aggregateType, id));
            var cachedValue = memoryCache.AddOrGetExisting(key, lazyValue, CreateCacheItemPolicy());

            return cachedValue as Aggregate ?? (cachedValue as Lazy<Aggregate> ?? lazyValue).Value;
        }

        /// <summary>
        /// Load the aggregate of the specified <paramref name="aggregateType"/> and aggregate <paramref name="id"/>.
        /// </summary>
        /// <param name="aggregateType">The type of aggregate to retrieve.</param>
        /// <param name="id">The unique aggregate id.</param>
        private Aggregate LoadAggregate(Type aggregateType, Guid id)
        {
            Aggregate aggregate;

            Log.TraceFormat("Aggregate {0}-{1} not found in cache.", aggregateType, id);

            aggregate = aggregateStore.Get(aggregateType, id);

            return aggregate;
        }

        /// <summary>
        /// Save the specified <paramref name="context"/> changes for the given aggregate.
        /// </summary>
        /// <param name="aggregate">The current aggregate version for which the context applies.</param>
        /// <param name="context">The command context containing the aggregate changes to be applied.</param>
        public SaveResult Save(Aggregate aggregate, CommandContext context)
        {
            Verify.NotNull(aggregate, "aggregate");
            Verify.NotNull(context, "context");

            var key = String.Concat(aggregate.GetType().FullName, "-", aggregate.Id.ToString());
            var copy = aggregate.Copy();

            try
            {
                var result = aggregateStore.Save(copy, context);

                memoryCache.Set(key, copy, CreateCacheItemPolicy());

                return result;
            }
            catch (ConcurrencyException)
            {
                // NOTE: Under normal configuration, this should not happen as commands against a specific
                //       aggregate are serialized. If a concurrency exception is thrown, then the conflict
                //       is likely at the persistence level and thus any cached instance of the aggregate 
                //       should be purged and re-cached on subsequent `Get` call.
                memoryCache.Remove(key);
                throw;
            }
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
            Log.TraceFormat("Aggregate {0} was removed: {1}.", e.CacheItem.Key, e.RemovedReason);
        }
    }
}
