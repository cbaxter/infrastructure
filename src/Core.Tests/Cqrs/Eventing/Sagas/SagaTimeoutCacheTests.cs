using System;
using System.Linq;
using Moq;
using Spark;
using Spark.Cqrs.Eventing.Sagas;
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

namespace Test.Spark.Cqrs.Eventing.Sagas
{
    namespace UsingSagaTimeoutCache
    {

        public abstract class UsingPopulatedSagaTimeoutCache
        {
            protected readonly Mock<IStoreSagas> SagaStore;
            protected readonly TimeSpan TimeoutCacheDuration;
            internal readonly SagaTimeoutCache SagaTimeoutCache;

            protected UsingPopulatedSagaTimeoutCache()
            {
                SagaStore = new Mock<IStoreSagas>();
                TimeoutCacheDuration = TimeSpan.FromMinutes(1);
                SagaTimeoutCache = new SagaTimeoutCache(SagaStore.Object, TimeoutCacheDuration);
            }
        }

        public class WhenGettingNextScheduledTimeout : IDisposable
        {
            public void Dispose() { SystemTime.ClearOverride(); }

            [Fact]
            public void ReturnDateTimeMinValIfCacheEmpty()
            {
                var cache = new SagaTimeoutCache(new Mock<IStoreSagas>().Object, TimeSpan.FromMinutes(1));

                Assert.Equal(DateTime.MinValue, cache.GetNextScheduledTimeout());
            }

            [Fact]
            public void ReturnNextScheduledTimeoutIfCacheHasItems()
            {
                var now = DateTime.UtcNow;
                var nextTimeout = now.AddMinutes(1);
                var sagaStore = new Mock<IStoreSagas>();
                var cache = new SagaTimeoutCache(sagaStore.Object, TimeSpan.FromMinutes(10));

                SystemTime.OverrideWith(() => now);
                sagaStore.Setup(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>())).Returns(new[] { 
                    new SagaTimeout(typeof(Saga), GuidStrategy.NewGuid(), nextTimeout), 
                    new SagaTimeout(typeof(Saga), GuidStrategy.NewGuid(), nextTimeout.AddMinutes(5))
                });

                //NOTE: Because we are using an overriden SystemTime; the get will not actually clear out the cached items.
                Assert.Equal(0, cache.GetElapsedTimeouts().Count());
                Assert.Equal(nextTimeout, cache.GetNextScheduledTimeout());
            }
        }

        public class WhenGettingElapsedTimeouts : IDisposable
        {
            public void Dispose() { SystemTime.ClearOverride(); }

            [Fact]
            public void ReturnEmptySetIfNoTimeoutsCached()
            {
                var now = DateTime.UtcNow;
                var futureTime = now.AddMinutes(5);
                var sagaStore = new Mock<IStoreSagas>();
                var cache = new SagaTimeoutCache(sagaStore.Object, TimeSpan.FromMinutes(20));

                SystemTime.OverrideWith(() => futureTime);
                sagaStore.Setup(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>())).Returns(new SagaTimeout[0]);

                Assert.Equal(0, cache.GetElapsedTimeouts().Count());
            }

            [Fact]
            public void DoNotCacheScheduledTimeoutsIfMaximumCachedTimeoutInFuture()
            {
                var now = DateTime.UtcNow;
                var futureTime = now.AddMinutes(5);
                var sagaStore = new Mock<IStoreSagas>();
                var cache = new SagaTimeoutCache(sagaStore.Object, TimeSpan.FromMinutes(20));

                sagaStore.Setup(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>())).Returns(new SagaTimeout[0]);
                SystemTime.OverrideWith(() => futureTime);
                cache.GetElapsedTimeouts();

                SystemTime.OverrideWith(() => now);
                cache.GetElapsedTimeouts();

                sagaStore.Verify(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>()), Times.Once());
            }

            [Fact]
            public void ReturnScheduledTimeoutsInPast()
            {
                var now = DateTime.UtcNow;
                var futureTime = now.AddMinutes(5);
                var sagaStore = new Mock<IStoreSagas>();
                var cache = new SagaTimeoutCache(sagaStore.Object, TimeSpan.FromMinutes(20));

                SystemTime.OverrideWith(() => futureTime);
                sagaStore.Setup(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>())).Returns(new[] { 
                    new SagaTimeout(typeof(Saga), GuidStrategy.NewGuid(),  now.AddMinutes(1)), 
                    new SagaTimeout( typeof(Saga), GuidStrategy.NewGuid(), now.AddMinutes(5)), 
                    new SagaTimeout( typeof(Saga),GuidStrategy.NewGuid(),  now.AddMinutes(10))
                });

                var elapsedTimeouts = cache.GetElapsedTimeouts().ToArray();
                Assert.Equal(2, elapsedTimeouts.Length);
                Assert.Equal(now.AddMinutes(1), elapsedTimeouts[0].Timeout);
                Assert.Equal(now.AddMinutes(5), elapsedTimeouts[1].Timeout);
            }

            [Fact]
            public void CanTolerateCachingSameSagaReferenceMoreThanOnce()
            {
                var now = DateTime.UtcNow;
                var futureTime = now.AddMinutes(10);
                var sagaStore = new Mock<IStoreSagas>();
                var cache = new SagaTimeoutCache(sagaStore.Object, TimeSpan.FromMinutes(5));
                var sagaTimeout = new SagaTimeout(typeof(Saga), GuidStrategy.NewGuid(), futureTime);

                sagaStore.Setup(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>())).Returns(new[] { sagaTimeout });

                SystemTime.OverrideWith(() => now);
                cache.GetElapsedTimeouts();

                SystemTime.OverrideWith(() => futureTime);
                cache.GetElapsedTimeouts();

                sagaStore.Verify(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>()), Times.Exactly(2));
            }
        }

        public class WhenSchedulingTimeout : IDisposable
        {
            public void Dispose() { SystemTime.ClearOverride(); }

            [Fact]
            public void CachePendingTimeouts()
            {
                var now = DateTime.UtcNow;
                var sagaStore = new Mock<IStoreSagas>();
                var cache = new SagaTimeoutCache(sagaStore.Object, TimeSpan.FromMinutes(5));
                var sagaTimeout = new SagaTimeout(typeof(Saga), GuidStrategy.NewGuid(), now.AddMinutes(1));

                sagaStore.Setup(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>())).Returns(new SagaTimeout[0]);

                SystemTime.OverrideWith(() => now);

                cache.GetElapsedTimeouts();
                cache.ScheduleTimeout(sagaTimeout);

                Assert.Equal(1, cache.Count);
            }

            [Fact]
            public void DoNotCacheTimeoutsFarInFuture()
            {
                var now = DateTime.UtcNow;
                var sagaStore = new Mock<IStoreSagas>();
                var cache = new SagaTimeoutCache(sagaStore.Object, TimeSpan.FromMinutes(5));
                var sagaTimeout = new SagaTimeout(typeof(Saga), GuidStrategy.NewGuid(), now.AddMinutes(10));

                sagaStore.Setup(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>())).Returns(new[] { sagaTimeout });

                SystemTime.OverrideWith(() => now);

                cache.GetElapsedTimeouts();
                cache.ScheduleTimeout(sagaTimeout);

                Assert.Equal(0, cache.Count);
            }
        }

        public class WhenClearingTimeouts
        {
            [Fact]
            public void IgnoreUnknownSagaReferences()
            {
                var sagaStore = new Mock<IStoreSagas>();
                var cache = new SagaTimeoutCache(sagaStore.Object, TimeSpan.FromMinutes(5));

                Assert.DoesNotThrow(() => cache.ClearTimeout(new SagaReference(typeof(Saga), GuidStrategy.NewGuid())));
            }

            [Fact]
            public void RemoveKnownSagaReferences()
            {
                var now = DateTime.UtcNow;
                var sagaStore = new Mock<IStoreSagas>();
                var cache = new SagaTimeoutCache(sagaStore.Object, TimeSpan.FromMinutes(5));
                var sagaTimeout = new SagaTimeout(typeof(Saga), GuidStrategy.NewGuid(), now.AddMinutes(10));

                sagaStore.Setup(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>())).Returns(new[] { sagaTimeout });

                SystemTime.OverrideWith(() => now);

                cache.ClearTimeout(new SagaReference(sagaTimeout.SagaType, sagaTimeout.SagaId));

                Assert.Equal(0, cache.Count);
            }
        }

        public class WhenClearingCache
        {
            [Fact]
            public void NextScheduledTimeoutResetToDateTimeMinvalue()
            {
                var cache = new SagaTimeoutCache(new Mock<IStoreSagas>().Object, TimeSpan.FromMinutes(1));

                cache.Clear();

                Assert.Equal(DateTime.MinValue, cache.GetNextScheduledTimeout());
            }
        }
    }
}
