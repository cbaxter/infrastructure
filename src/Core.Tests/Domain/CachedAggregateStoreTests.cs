using System;
using System.Runtime.Caching;
using Moq;
using Spark.Commanding;
using Spark.Domain;
using Spark.EventStore;
using Spark.Messaging;
using Xunit;

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

namespace Spark.Tests.Domain
{
    public static class UsingCachedAggregateStore
    {
        public class WhenGettingAggregate
        {
            [Fact]
            public void UseCachedAggregateIfAvailable()
            {
                var aggregate = new FakeAggregate();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var memoryCache = new MemoryCache(Guid.NewGuid().ToString());
                var cachedAggregateStore = new CachedAggregateStore(decoratedAggregateStore.Object, TimeSpan.FromMinutes(1), memoryCache);

                memoryCache.Add(aggregate.CacheKey, aggregate, new CacheItemPolicy());

                Assert.Same(aggregate, cachedAggregateStore.Get(typeof(FakeAggregate), aggregate.Id));

                decoratedAggregateStore.Verify(mock => mock.Get(typeof(FakeAggregate), aggregate.Id), Times.Never());
            }

            [Fact]
            public void LoadFromUnderlyingStoreIfNotCached()
            {
                var aggregate = new FakeAggregate();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var memoryCache = new MemoryCache(Guid.NewGuid().ToString());
                var cachedAggregateStore = new CachedAggregateStore(decoratedAggregateStore.Object, TimeSpan.FromMinutes(1), memoryCache);

                decoratedAggregateStore.Setup(mock => mock.Get(typeof(FakeAggregate), aggregate.Id)).Returns(aggregate);

                cachedAggregateStore.Get(typeof(FakeAggregate), aggregate.Id);

                decoratedAggregateStore.Verify(mock => mock.Get(typeof(FakeAggregate), aggregate.Id), Times.Once());
            }

            [Fact]
            public void CanUnwrapLazyCachedValue()
            {
                var aggregate = new FakeAggregate();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var memoryCache = new MemoryCache(Guid.NewGuid().ToString());
                var cachedAggregateStore = new CachedAggregateStore(decoratedAggregateStore.Object, TimeSpan.FromMinutes(1), memoryCache);

                memoryCache.Add(aggregate.CacheKey, new Lazy<Aggregate>(() => aggregate), new CacheItemPolicy());

                Assert.DoesNotThrow(() => cachedAggregateStore.Get(typeof(FakeAggregate), aggregate.Id));
            }

            [Fact]
            public void CanUseRawCachedValue()
            {
                var aggregate = new FakeAggregate();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var memoryCache = new MemoryCache(Guid.NewGuid().ToString());
                var cachedAggregateStore = new CachedAggregateStore(decoratedAggregateStore.Object, TimeSpan.FromMinutes(1), memoryCache);

                memoryCache.Add(aggregate.CacheKey, new FakeAggregate(), new CacheItemPolicy());

                Assert.DoesNotThrow(() => cachedAggregateStore.Get(typeof(FakeAggregate), aggregate.Id));
            }
        }

        public class WhenSavingAggregate
        {
            [Fact]
            public void CopyAggregateBeforeSaving()
            {
                var aggregate = new FakeAggregate();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var memoryCache = new MemoryCache(Guid.NewGuid().ToString());
                var cachedAggregateStore = new CachedAggregateStore(decoratedAggregateStore.Object, TimeSpan.FromMinutes(1), memoryCache);

                // ReSharper disable AccessToDisposedClosure
                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                {
                    cachedAggregateStore.Save(aggregate, context);
                    decoratedAggregateStore.Verify(mock => mock.Save(It.Is<Aggregate>(copy => !ReferenceEquals(aggregate, copy)), context), Times.Once());
                }
                // ReSharper restore AccessToDisposedClosure
            }

            [Fact]
            public void UpdateCacheOnSuccessfulSave()
            {
                var aggregate = new FakeAggregate();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var memoryCache = new MemoryCache(Guid.NewGuid().ToString());
                var cachedAggregateStore = new CachedAggregateStore(decoratedAggregateStore.Object, TimeSpan.FromMinutes(1), memoryCache);

                memoryCache.Add(aggregate.CacheKey, aggregate, new CacheItemPolicy());

                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                    cachedAggregateStore.Save(aggregate, context);

                Assert.NotSame(aggregate, memoryCache.Get(aggregate.CacheKey));
            }

            [Fact]
            public void RemoveAggregateFromCacheOnConcurrencyException()
            {
                var aggregate = new FakeAggregate();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var memoryCache = new MemoryCache(Guid.NewGuid().ToString());
                var cachedAggregateStore = new CachedAggregateStore(decoratedAggregateStore.Object, TimeSpan.FromMinutes(1), memoryCache);

                // ReSharper disable AccessToDisposedClosure
                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                {
                    memoryCache.Add(aggregate.CacheKey, aggregate, new CacheItemPolicy());
                    decoratedAggregateStore.Setup(mock => mock.Save(It.Is<Aggregate>(copy => !ReferenceEquals(aggregate, copy)), context)).Throws<ConcurrencyException>();

                    Assert.Throws<ConcurrencyException>(() => cachedAggregateStore.Save(aggregate, context));
                    Assert.False(memoryCache.Contains(aggregate.CacheKey));
                }
                // ReSharper restore AccessToDisposedClosure
            }
        }

        public class WhenDisposing
        {
            [Fact]
            public void CanCallDisposeMoreThanOnce()
            {
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var cachedAggregateStore = new CachedAggregateStore(decoratedAggregateStore.Object);

                cachedAggregateStore.Dispose();

                Assert.DoesNotThrow(() => cachedAggregateStore.Dispose());
            }
        }

        private class FakeAggregate : Aggregate
        {
            public String CacheKey { get { return String.Concat(GetType().FullName, "-", Id.ToString()); } }
            public FakeAggregate() { Id = GuidStrategy.NewGuid(); }
        }
    }
}
