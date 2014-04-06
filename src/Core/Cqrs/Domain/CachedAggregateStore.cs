﻿using System;
using System.Runtime.Caching;
using Spark.Cqrs.Commanding;
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

namespace Spark.Cqrs.Domain
{
    /// <summary>
    /// A <see cref="IStoreAggregates"/> wrapper class to enable <see cref="MemoryCache"/> storage of <see cref="Aggregate"/> instances to reduce <see cref="Get"/> overhead.
    /// </summary>
    public sealed class CachedAggregateStore : IStoreAggregates
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IStoreAggregates aggregateStore;
        private readonly TimeSpan slidingExpiration;
        private readonly MemoryCache memoryCache;

        /// <summary>
        /// Initializes a new instance of <see cref="CachedAggregateStore"/>.
        /// </summary>
        /// <param name="aggregateStore">The underlying <see cref="IStoreAggregates"/> implementation to be decorated.</param>
        public CachedAggregateStore(IStoreAggregates aggregateStore)
            : this(aggregateStore, Settings.AggregateStore.CacheSlidingExpiration, new MemoryCache("AggregateCache"))
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="CachedAggregateStore"/> using the specified <paramref name="slidingExpiration"/>.
        /// </summary>
        /// <param name="aggregateStore">The underlying <see cref="IStoreAggregates"/> implementation to be decorated.</param>
        /// <param name="slidingExpiration">The maximum time an <see cref="Aggregate"/> may existing in the cache without being accessed.</param>
        /// <param name="memoryCache">The underlying cache implementation.</param>
        internal CachedAggregateStore(IStoreAggregates aggregateStore, TimeSpan slidingExpiration, MemoryCache memoryCache)
        {
            Verify.NotNull(memoryCache, "memoryCache");
            Verify.NotNull(aggregateStore, "aggregateStore");
            Verify.GreaterThanOrEqual(TimeSpan.FromSeconds(1), slidingExpiration, "aggregateStore");

            this.memoryCache = memoryCache;
            this.aggregateStore = aggregateStore;
            this.slidingExpiration = slidingExpiration;
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="CachedAggregateStore"/> class.
        /// </summary>
        public void Dispose()
        {
            aggregateStore.Dispose();
            memoryCache.Dispose();
        }

        /// <summary>
        /// Retrieve the aggregate of the specified <paramref name="aggregateType"/> and aggregate <paramref name="id"/>.
        /// </summary>
        /// <param name="aggregateType">The type of aggregate to retrieve.</param>
        /// <param name="id">The unique aggregate id.</param>
        public Aggregate Get(Type aggregateType, Guid id)
        {
            Verify.NotNull(aggregateType, "aggregateType");

            var key = String.Concat(aggregateType.GetFullNameWithAssembly(), "-", id);
            using (var aggregateLock = new AggregateLock(aggregateType, id))
            {
                aggregateLock.Aquire();

                //NOTE: We do not want to use AddOrGetExisting due to internal global cache lock while doing aggregate lookup.
                var aggregate = (Aggregate)memoryCache.Get(key);
                if (aggregate == null)
                    memoryCache.Add(key, aggregate = aggregateStore.Get(aggregateType, id), CreateCacheItemPolicy());

                //NOTE: Given that aggregate state is only applied during `Save`, we can return the cached instance.
                //      This avoids making a copy of the aggregate when no state changes will be applied.
                return aggregate;
            }
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

            var copy = aggregate.Copy();
            var aggregateType = aggregate.GetType();
            var key = String.Concat(aggregateType.GetFullNameWithAssembly(), "-", aggregate.Id);
            using (var aggregateLock = new AggregateLock(aggregateType, aggregate.Id))
            {
                aggregateLock.Aquire();

                try
                {
                    var result = aggregateStore.Save(copy, context);

                    memoryCache.Set(key, copy, CreateCacheItemPolicy());

                    return result;
                }
                catch (ConcurrencyException)
                {
                    memoryCache.Remove(key);
                    throw;
                }
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
