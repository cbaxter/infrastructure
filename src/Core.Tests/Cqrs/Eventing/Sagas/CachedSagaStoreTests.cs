using Moq;
using Spark;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Data;
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
    public static class UsingCachedSagaStore
    {
        public class WhenCreatingSaga
        {
            [Fact]
            public void DelegateToDecoratedSagaStore()
            {
                var sagaStore = new Mock<IStoreSagas>();
                var sagaId = GuidStrategy.NewGuid();

                using (var cachedSagaStore = new CachedSagaStore(sagaStore.Object))
                {
                    cachedSagaStore.CreateSaga(typeof(Saga), sagaId);

                    sagaStore.Verify(mock => mock.CreateSaga(typeof(Saga), sagaId), Times.Once());
                }
            }
        }

        public class WhenTryingToRetrieveCachedSaga
        {
            [Fact]
            public void CacheAndReuseExistingSaga()
            {
                var sagaStore = new Mock<IStoreSagas>();
                var sagaId = GuidStrategy.NewGuid();

                using (var cachedSagaStore = new CachedSagaStore(sagaStore.Object))
                {
                    Saga cachedSaga = new FakeSaga();

                    sagaStore.Setup(mock => mock.TryGetSaga(typeof(Saga), sagaId, out cachedSaga)).Returns(true);

                    Assert.True(cachedSagaStore.TryGetSaga(typeof(Saga), sagaId, out cachedSaga));
                    Assert.True(cachedSagaStore.TryGetSaga(typeof(Saga), sagaId, out cachedSaga));

                    sagaStore.Verify(mock => mock.TryGetSaga(typeof(Saga), sagaId, out cachedSaga), Times.Once());
                }
            }

            [Fact]
            public void NullSagaNotCached()
            {
                var sagaStore = new Mock<IStoreSagas>();
                var sagaId = GuidStrategy.NewGuid();

                using (var cachedSagaStore = new CachedSagaStore(sagaStore.Object))
                {
                    Saga cachedSaga = default(Saga);

                    sagaStore.Setup(mock => mock.TryGetSaga(typeof(Saga), sagaId, out cachedSaga)).Returns(false);

                    Assert.False(cachedSagaStore.TryGetSaga(typeof(Saga), sagaId, out cachedSaga));
                    Assert.False(cachedSagaStore.TryGetSaga(typeof(Saga), sagaId, out cachedSaga));

                    sagaStore.Verify(mock => mock.TryGetSaga(typeof(Saga), sagaId, out cachedSaga), Times.Exactly(2));
                }
            }

            private class FakeSaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                { }
            }
        }

        // ReSharper disable AccessToDisposedClosure
        public class WhenSavingSaga
        {
            [Fact]
            public void SagaStateCopiedBeforeSaving()
            {
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid() };
                var sagaStore = new Mock<IStoreSagas>();

                using (var sagaContext = new SagaContext(typeof(FakeSaga), saga.CorrelationId, new FakeEvent()))
                using (var cachedSagaStore = new CachedSagaStore(sagaStore.Object))
                {
                    cachedSagaStore.Save(saga, sagaContext);

                    sagaStore.Verify(mock => mock.Save(It.Is<Saga>(copy => !ReferenceEquals(saga, copy)), sagaContext), Times.Once());
                }
            }

            [Fact]
            public void SagaUpdatedAfterSaveIfNotCompleted()
            {
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid() };
                var sagaStore = new Mock<IStoreSagas>();
                var sagaCopy = default(FakeSaga);
                var cachedSaga = default(Saga);

                using (var sagaContext = new SagaContext(typeof(FakeSaga), saga.CorrelationId, new FakeEvent()))
                using (var cachedSagaStore = new CachedSagaStore(sagaStore.Object))
                {
                    sagaStore.Setup(mock => mock.Save(It.IsAny<Saga>(), sagaContext)).Callback((Saga s, SagaContext c) => { sagaCopy = (FakeSaga)s; });

                    cachedSagaStore.Save(saga, sagaContext);


                    Assert.True(cachedSagaStore.TryGetSaga(typeof(FakeSaga), saga.CorrelationId, out cachedSaga));
                    Assert.Same(sagaCopy, cachedSaga);
                }
            }

            [Fact]
            public void SagaRemovedFromCacheIfCompleted()
            {
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid() };
                var sagaStore = new Mock<IStoreSagas>();
                var cachedSaga = default(Saga);

                using (var sagaContext = new SagaContext(typeof(FakeSaga), saga.CorrelationId, new FakeEvent()))
                using (var cachedSagaStore = new CachedSagaStore(sagaStore.Object))
                {
                    cachedSagaStore.Save(saga, sagaContext);

                    saga.Completed = true;

                    cachedSagaStore.Save(saga, sagaContext);

                    Assert.False(cachedSagaStore.TryGetSaga(typeof(FakeSaga), saga.CorrelationId, out cachedSaga));
                }
            }

            [Fact]
            public void SagaRemovedFromCacheIfConcurrencyExceptionThrown()
            {
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid() };
                var sagaStore = new Mock<IStoreSagas>();
                var cachedSaga = default(Saga);
                var save = 0;

                using (var sagaContext = new SagaContext(typeof(FakeSaga), saga.CorrelationId, new FakeEvent()))
                using (var cachedSagaStore = new CachedSagaStore(sagaStore.Object))
                {
                    sagaStore.Setup(mock => mock.Save(It.IsAny<Saga>(), sagaContext)).Callback(() => { if (save++ == 1) { throw new ConcurrencyException(); } });
                    cachedSagaStore.Save(saga, sagaContext);

                    Assert.True(cachedSagaStore.TryGetSaga(typeof(FakeSaga), saga.CorrelationId, out cachedSaga));
                    Assert.Throws<ConcurrencyException>(() => cachedSagaStore.Save(saga, sagaContext));
                    Assert.False(cachedSagaStore.TryGetSaga(typeof(FakeSaga), saga.CorrelationId, out cachedSaga));
                }
            }

            private class FakeSaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                { }
            }

            private class FakeEvent : Event
            { }
        }
        // ReSharper restore AccessToDisposedClosure

        public class WhenPurgingSagas
        {
            [Fact]
            public void DelegateToDecoratedSagaStore()
            {
                var sagaStore = new Mock<IStoreSagas>();

                using (var cachedSagaStore = new CachedSagaStore(sagaStore.Object))
                {
                    cachedSagaStore.Purge();

                    sagaStore.Verify(mock => mock.Purge(), Times.Once());
                }
            }
        }
    }
}
