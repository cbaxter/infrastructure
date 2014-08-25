using System;
using Moq;
using Spark;
using Spark.Cqrs.Eventing;
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
    namespace UsingHookableSagaStore
    {
        public class WhenCreatingSagaStore
        {
            [Fact]
            public void OrderPreGetPipelineHooksByOrderThenByName()
            {
                var pipelineHooks = new PipelineHook[] { new PipelineHookC(), new PipelineHookA(), new PipelineHookB() };
                var sagaStore = new HookableSagaStore(new Mock<IStoreSagas>().Object, pipelineHooks);
                var preGetHooks = sagaStore.PreGetHooks.AsList();

                Assert.IsType<PipelineHookB>(preGetHooks[0]);
                Assert.IsType<PipelineHookC>(preGetHooks[1]);
                Assert.IsType<PipelineHookA>(preGetHooks[2]);
            }

            [Fact]
            public void OrderPostGetPipelineHooksByOrderThenByNameReversed()
            {
                var pipelineHooks = new PipelineHook[] { new PipelineHookC(), new PipelineHookA(), new PipelineHookB() };
                var sagaStore = new HookableSagaStore(new Mock<IStoreSagas>().Object, pipelineHooks);
                var preGetHooks = sagaStore.PostGetHooks.AsList();

                Assert.IsType<PipelineHookA>(preGetHooks[0]);
                Assert.IsType<PipelineHookC>(preGetHooks[1]);
                Assert.IsType<PipelineHookB>(preGetHooks[2]);
            }
            [Fact]
            public void OrderPreSavePipelineHooksByOrderThenByName()
            {
                var pipelineHooks = new PipelineHook[] { new PipelineHookC(), new PipelineHookA(), new PipelineHookB() };
                var sagaStore = new HookableSagaStore(new Mock<IStoreSagas>().Object, pipelineHooks);
                var preGetHooks = sagaStore.PreSaveHooks.AsList();

                Assert.IsType<PipelineHookB>(preGetHooks[0]);
                Assert.IsType<PipelineHookC>(preGetHooks[1]);
                Assert.IsType<PipelineHookA>(preGetHooks[2]);
            }

            [Fact]
            public void OrderPostSavePipelineHooksByOrderThenByNameReversed()
            {
                var pipelineHooks = new PipelineHook[] { new PipelineHookC(), new PipelineHookA(), new PipelineHookB() };
                var sagaStore = new HookableSagaStore(new Mock<IStoreSagas>().Object, pipelineHooks);
                var preGetHooks = sagaStore.PostSaveHooks.AsList();

                Assert.IsType<PipelineHookA>(preGetHooks[0]);
                Assert.IsType<PipelineHookC>(preGetHooks[1]);
                Assert.IsType<PipelineHookB>(preGetHooks[2]);
            }

            private class PipelineHookA : PipelineHook
            {
                public PipelineHookA() : base(2) { }
                public override void PreGet(Type sagaType, Guid id) { }
                public override void PostGet(Saga saga) { }
                public override void PreSave(Saga saga, SagaContext context) { }
                public override void PostSave(Saga saga, SagaContext commit, Exception error) { }
            }

            private class PipelineHookB : PipelineHook
            {
                public PipelineHookB() : base(1) { }
                public override void PreGet(Type sagaType, Guid id) { }
                public override void PostGet(Saga saga) { }
                public override void PreSave(Saga saga, SagaContext context) { }
                public override void PostSave(Saga saga, SagaContext commit, Exception error) { }
            }

            private class PipelineHookC : PipelineHook
            {
                public PipelineHookC() : base(1) { }
                public override void PreGet(Type sagaType, Guid id) { }
                public override void PostGet(Saga saga) { }
                public override void PreSave(Saga saga, SagaContext context) { }
                public override void PostSave(Saga saga, SagaContext commit, Exception error) { }
            }
        }

        public class WhenCreatingSaga
        {
            [Fact]
            public void InvokePreGetHooksBeforeDecoratedGet()
            {
                var id = Guid.NewGuid();
                var type = typeof(Saga);
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedSagaStore = new Mock<IStoreSagas>();
                var sagaStore = new HookableSagaStore(decoratedSagaStore.Object, new[] { pipelineHook.Object });

                pipelineHook.Setup(mock => mock.PreGet(type, id)).Throws(new InvalidOperationException());

                Assert.Throws<InvalidOperationException>(() => sagaStore.CreateSaga(type, id));

                decoratedSagaStore.Verify(mock => mock.CreateSaga(type, id), Times.Never());
            }

            [Fact]
            public void InvokePostGetHooksAfterDecoratedGet()
            {
                var id = Guid.NewGuid();
                var type = typeof(Saga);
                var saga = new Mock<Saga>();
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedSagaStore = new Mock<IStoreSagas>();
                var sagaStore = new HookableSagaStore(decoratedSagaStore.Object, new[] { pipelineHook.Object });

                decoratedSagaStore.Setup(mock => mock.CreateSaga(type, id)).Returns(saga.Object);
                pipelineHook.Setup(mock => mock.PostGet(saga.Object)).Throws(new InvalidOperationException());

                Assert.Throws<InvalidOperationException>(() => sagaStore.CreateSaga(type, id));

                decoratedSagaStore.Verify(mock => mock.CreateSaga(type, id), Times.Once());
            }

            [Fact]
            public void ReturnSagaIfNoExceptionsThrown()
            {
                var id = Guid.NewGuid();
                var type = typeof(Saga);
                var saga = new Mock<Saga>();
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedSagaStore = new Mock<IStoreSagas>();
                var sagaStore = new HookableSagaStore(decoratedSagaStore.Object, new[] { pipelineHook.Object });

                decoratedSagaStore.Setup(mock => mock.CreateSaga(type, id)).Returns(saga.Object);

                Assert.Same(saga.Object, sagaStore.CreateSaga(type, id));
            }
        }

        public class WhenGettingSaga
        {
            [Fact]
            public void InvokePreGetHooksBeforeDecoratedGet()
            {
                var id = Guid.NewGuid();
                var type = typeof(Saga);
                var saga = default(Saga);
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedSagaStore = new Mock<IStoreSagas>();
                var sagaStore = new HookableSagaStore(decoratedSagaStore.Object, new[] { pipelineHook.Object });

                pipelineHook.Setup(mock => mock.PreGet(type, id)).Throws(new InvalidOperationException());

                Assert.Throws<InvalidOperationException>(() => sagaStore.TryGetSaga(type, id, out saga));

                decoratedSagaStore.Verify(mock => mock.TryGetSaga(type, id, out saga), Times.Never());
            }

            [Fact]
            public void InvokePostGetHooksAfterDecoratedGet()
            {
                var id = Guid.NewGuid();
                var type = typeof(Saga);
                var saga = default(Saga);
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedSagaStore = new Mock<IStoreSagas>();
                var sagaStore = new HookableSagaStore(decoratedSagaStore.Object, new[] { pipelineHook.Object });

                decoratedSagaStore.Setup(mock => mock.TryGetSaga(type, id, out saga)).Returns(true);
                pipelineHook.Setup(mock => mock.PostGet(saga)).Throws(new InvalidOperationException());

                Assert.Throws<InvalidOperationException>(() => sagaStore.TryGetSaga(type, id, out saga));

                decoratedSagaStore.Verify(mock => mock.TryGetSaga(type, id, out saga), Times.Once());
            }

            [Fact]
            public void ReturnSagaIfNoExceptionsThrown()
            {
                var id = Guid.NewGuid();
                var type = typeof(Saga);
                var saga = default(Saga);
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedSagaStore = new Mock<IStoreSagas>();
                var sagaStore = new HookableSagaStore(decoratedSagaStore.Object, new[] { pipelineHook.Object });

                decoratedSagaStore.Setup(mock => mock.TryGetSaga(type, id, out saga)).Returns(true);

                Assert.True(sagaStore.TryGetSaga(type, id, out saga));
            }
        }

        public class WhenGettingScheduledTimeouts
        {
            [Fact]
            public void DelegateToDecoratedSagaStore()
            {
                var sagaStore = new Mock<IStoreSagas>();
                var upperBound = DateTime.Now;

                using (var cachedSagaStore = new HookableSagaStore(sagaStore.Object, new PipelineHook[0]))
                {
                    cachedSagaStore.GetScheduledTimeouts(upperBound);

                    sagaStore.Verify(mock => mock.GetScheduledTimeouts(upperBound), Times.Once());
                }
            }
        }

        public class WhenSavingSaga
        {
            [Fact]
            public void InvokePreSaveHooksBeforeDecoratedSave()
            {
                var saga = new Mock<Saga>();
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedSagaStore = new Mock<IStoreSagas>();
                var sagaStore = new HookableSagaStore(decoratedSagaStore.Object, new[] { pipelineHook.Object });

                // ReSharper disable AccessToDisposedClosure
                using (var context = new SagaContext(typeof(Saga), Guid.NewGuid(), new FakeEvent()))
                {
                    pipelineHook.Setup(mock => mock.PreSave(saga.Object, context)).Throws(new InvalidOperationException());

                    Assert.Throws<InvalidOperationException>(() => sagaStore.Save(saga.Object, context));

                    decoratedSagaStore.Verify(mock => mock.Save(saga.Object, context), Times.Never());
                }
                // ReSharper restore AccessToDisposedClosure
            }

            [Fact]
            public void InvokePostSaveHooksAfterDecoratedSave()
            {
                var saga = new Mock<Saga>();
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedSagaStore = new Mock<IStoreSagas>();
                var sagaStore = new HookableSagaStore(decoratedSagaStore.Object, new[] { pipelineHook.Object });


                // ReSharper disable AccessToDisposedClosure
                using (var context = new SagaContext(typeof(Saga), Guid.NewGuid(), new FakeEvent()))
                {
                    decoratedSagaStore.Setup(mock => mock.Save(saga.Object, context)).Returns(saga.Object);
                    pipelineHook.Setup(mock => mock.PostSave(saga.Object, context, null)).Throws(new InvalidOperationException());

                    Assert.Throws<InvalidOperationException>(() => sagaStore.Save(saga.Object, context));

                    decoratedSagaStore.Verify(mock => mock.Save(saga.Object, context), Times.Once());
                }
                // ReSharper restore AccessToDisposedClosure
            }

            [Fact]
            public void InvokePostSaveHooksIfDecoratedSaveThrowsException()
            {
                var saga = new Mock<Saga>();
                var pipelineHook = new Mock<PipelineHook>();
                var error = new InvalidOperationException();
                var decoratedSagaStore = new Mock<IStoreSagas>();
                var sagaStore = new HookableSagaStore(decoratedSagaStore.Object, new[] { pipelineHook.Object });

                // ReSharper disable AccessToDisposedClosure
                using (var context = new SagaContext(typeof(Saga), Guid.NewGuid(), new FakeEvent()))
                {
                    decoratedSagaStore.Setup(mock => mock.Save(saga.Object, context)).Throws(error);

                    Assert.Throws<InvalidOperationException>(() => sagaStore.Save(saga.Object, context));

                    pipelineHook.Verify(mock => mock.PostSave(saga.Object, context, error), Times.Once());
                }
                // ReSharper restore AccessToDisposedClosure
            }

            [Fact]
            public void ReturnSaveResultIfNoExceptionsThrown()
            {
                var saga = new Mock<Saga>();
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedSagaStore = new Mock<IStoreSagas>();
                var sagaStore = new HookableSagaStore(decoratedSagaStore.Object, new[] { pipelineHook.Object });

                // ReSharper disable AccessToDisposedClosure
                using (var context = new SagaContext(typeof(Saga), Guid.NewGuid(), new FakeEvent()))
                {
                    decoratedSagaStore.Setup(mock => mock.Save(saga.Object, context)).Returns(saga.Object);

                    Assert.Same(saga.Object, sagaStore.Save(saga.Object, context));
                }
                // ReSharper restore AccessToDisposedClosure
            }

            private class FakeEvent : Event { }
        }

        public class WhenPurging
        {
            [Fact]
            public void DelegateToDecoratedSagaStore()
            {
                var sagaStore = new Mock<IStoreSagas>();

                using (var cachedSagaStore = new HookableSagaStore(sagaStore.Object, new PipelineHook[0]))
                {
                    cachedSagaStore.Purge();

                    sagaStore.Verify(mock => mock.Purge(), Times.Once());
                }
            }
        }

        public class WhenDisposing
        {
            [Fact]
            public void CanCallDisposeMoreThanOnce()
            {
                var pipelineHook = new DisposablePipelineHook();
                var pipelineHooks = new PipelineHook[] { pipelineHook };
                var sagaStore = new HookableSagaStore(new Mock<IStoreSagas>().Object, pipelineHooks);

                sagaStore.Dispose();
                sagaStore.Dispose();

                Assert.True(pipelineHook.Disposed);
            }

            private class DisposablePipelineHook : PipelineHook
            {
                public Boolean Disposed { get; private set; }

                public override void PreGet(Type sagaType, Guid id)
                { }

                protected override void Dispose(Boolean disposing)
                {
                    base.Dispose(disposing);
                    Disposed = true;
                }
            }
        }
    }
}
