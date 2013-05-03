using System;
using System.Runtime.Caching;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Logging;

namespace Spark.Infrastructure.Domain
{
    public sealed class CachedAggregateStore : IStoreAggregates, IDisposable //TODO: make all stores disposable (and piplineHooks)?
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IStoreAggregates aggregateStore;
        private readonly TimeSpan slidingExpiration;
        private readonly MemoryCache memoryCache;
        private Boolean disposed;

        public CachedAggregateStore(IStoreAggregates aggregateStore)
            : this(aggregateStore, Settings.AggregateStore.CacheSlidingExpiration)
        {
            Verify.NotNull(aggregateStore, "aggregateStore");

            this.aggregateStore = aggregateStore;
            this.memoryCache = new MemoryCache("AggregateCache");
        }

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

        public Aggregate Get(Type aggregateType, Guid id)
        {
            Verify.NotNull(aggregateType, "aggregateType");

            var key = String.Concat(aggregateType.FullName, "-", id.ToString());
            var lazyValue = new Lazy<Aggregate>(() => LoadAggregate(aggregateType, id));
            var cachedValue = memoryCache.AddOrGetExisting(key, lazyValue, CreateCacheItemPolicy());

            return cachedValue as Aggregate ?? (cachedValue as Lazy<Aggregate> ?? lazyValue).Value;
        }

        private Aggregate LoadAggregate(Type aggregateType, Guid id)
        {
            Aggregate aggregate;

            Log.TraceFormat("Aggregate {0}-{1} not found in cache.", aggregateType, id);

            aggregate = aggregateStore.Get(aggregateType, id);

            return aggregate;
        }

        public Commit Save(Aggregate aggregate, CommandContext context)
        {
            Verify.NotNull(aggregate, "aggregate");
            Verify.NotNull(context, "context");

            var key = String.Concat(aggregate.GetType().FullName, "-", aggregate.Id.ToString());
            var copy = aggregate.Copy();

            try
            {
                var commit = aggregateStore.Save(copy, context);

                memoryCache.Set(key, copy, CreateCacheItemPolicy());

                return commit;
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

        private CacheItemPolicy CreateCacheItemPolicy()
        {
            var cacheItemPolicy = new CacheItemPolicy { SlidingExpiration = slidingExpiration, RemovedCallback = OnCacheItemRemoved };

            return cacheItemPolicy;
        }

        private static void OnCacheItemRemoved(CacheEntryRemovedArguments e)
        {
            Log.TraceFormat("Aggregate {0} was removed: {1}.", e.CacheItem.Key, e.RemovedReason);
        }
    }
}
