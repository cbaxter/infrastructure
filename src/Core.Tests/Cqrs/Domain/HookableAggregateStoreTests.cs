using System;
using System.Runtime.Serialization;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.EventStore;
using Spark.Messaging;
using Xunit;

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

namespace Test.Spark.Cqrs.Domain
{
    namespace UsingHookableAggregateStore
    {
        public class WhenCreatingAggregateStore
        {
            [Fact]
            public void OrderPreGetPipelineHooksByOrderThenByName()
            {
                var pipelineHooks = new PipelineHook[] { new PipelineHookC(), new PipelineHookA(), new PipelineHookB() };
                var aggregateStore = new HookableAggregateStore(new Mock<IStoreAggregates>().Object, pipelineHooks);
                var preGetHooks = aggregateStore.PreGetHooks.AsList();

                Assert.IsType(typeof(PipelineHookB), preGetHooks[0]);
                Assert.IsType(typeof(PipelineHookC), preGetHooks[1]);
                Assert.IsType(typeof(PipelineHookA), preGetHooks[2]);
            }

            [Fact]
            public void OrderPostGetPipelineHooksByOrderThenByNameReversed()
            {
                var pipelineHooks = new PipelineHook[] { new PipelineHookC(), new PipelineHookA(), new PipelineHookB() };
                var aggregateStore = new HookableAggregateStore(new Mock<IStoreAggregates>().Object, pipelineHooks);
                var postGetHooks = aggregateStore.PostGetHooks.AsList();

                Assert.IsType(typeof(PipelineHookA), postGetHooks[0]);
                Assert.IsType(typeof(PipelineHookC), postGetHooks[1]);
                Assert.IsType(typeof(PipelineHookB), postGetHooks[2]);
            }
            [Fact]
            public void OrderPreSavePipelineHooksByOrderThenByName()
            {
                var pipelineHooks = new PipelineHook[] { new PipelineHookC(), new PipelineHookA(), new PipelineHookB() };
                var aggregateStore = new HookableAggregateStore(new Mock<IStoreAggregates>().Object, pipelineHooks);
                var preSaveHooks = aggregateStore.PreSaveHooks.AsList();

                Assert.IsType(typeof(PipelineHookB), preSaveHooks[0]);
                Assert.IsType(typeof(PipelineHookC), preSaveHooks[1]);
                Assert.IsType(typeof(PipelineHookA), preSaveHooks[2]);
            }

            [Fact]
            public void OrderPostSavePipelineHooksByOrderThenByNameReversed()
            {
                var pipelineHooks = new PipelineHook[] { new PipelineHookC(), new PipelineHookA(), new PipelineHookB() };
                var aggregateStore = new HookableAggregateStore(new Mock<IStoreAggregates>().Object, pipelineHooks);
                var postSaveHooks = aggregateStore.PostSaveHooks.AsList();

                Assert.IsType(typeof(PipelineHookA), postSaveHooks[0]);
                Assert.IsType(typeof(PipelineHookC), postSaveHooks[1]);
                Assert.IsType(typeof(PipelineHookB), postSaveHooks[2]);
            }

            private class PipelineHookA : PipelineHook
            {
                public PipelineHookA() : base(2) { }
                public override void PreGet(Type aggregateType, Guid id) { }
                public override void PostGet(Aggregate aggregate) { }
                public override void PreSave(Aggregate aggregate, CommandContext context) { }
                public override void PostSave(Aggregate aggregate, Commit commit, Exception error) { }
            }

            private class PipelineHookB : PipelineHook
            {
                public PipelineHookB() : base(1) { }
                public override void PreGet(Type aggregateType, Guid id) { }
                public override void PostGet(Aggregate aggregate) { }
                public override void PreSave(Aggregate aggregate, CommandContext context) { }
                public override void PostSave(Aggregate aggregate, Commit commit, Exception error) { }
            }

            private class PipelineHookC : PipelineHook
            {
                public PipelineHookC() : base(1) { }
                public override void PreGet(Type aggregateType, Guid id) { }
                public override void PostGet(Aggregate aggregate) { }
                public override void PreSave(Aggregate aggregate, CommandContext context) { }
                public override void PostSave(Aggregate aggregate, Commit commit, Exception error) { }
            }
        }

        public class WhenGettingAggregate
        {
            [Fact]
            public void InvokePreGetHooksBeforeDecoratedGet()
            {
                var id = Guid.NewGuid();
                var type = typeof(Aggregate);
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var aggregateStore = new HookableAggregateStore(decoratedAggregateStore.Object, new[] { pipelineHook.Object });

                pipelineHook.Setup(mock => mock.PreGet(type, id)).Throws(new InvalidOperationException());

                Assert.Throws<InvalidOperationException>(() => aggregateStore.Get(type, id));

                decoratedAggregateStore.Verify(mock => mock.Get(type, id), Times.Never());
            }

            [Fact]
            public void InvokePostGetHooksAfterDecoratedGet()
            {
                var id = Guid.NewGuid();
                var type = typeof(Aggregate);
                var aggregate = new Mock<Aggregate>();
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var aggregateStore = new HookableAggregateStore(decoratedAggregateStore.Object, new[] { pipelineHook.Object });

                decoratedAggregateStore.Setup(mock => mock.Get(type, id)).Returns(aggregate.Object);
                pipelineHook.Setup(mock => mock.PostGet(aggregate.Object)).Throws(new InvalidOperationException());

                Assert.Throws<InvalidOperationException>(() => aggregateStore.Get(type, id));

                decoratedAggregateStore.Verify(mock => mock.Get(type, id), Times.Once());
            }

            [Fact]
            public void ReturnAggregateIfNoExceptionsThrown()
            {
                var id = Guid.NewGuid();
                var type = typeof(Aggregate);
                var aggregate = new Mock<Aggregate>();
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var aggregateStore = new HookableAggregateStore(decoratedAggregateStore.Object, new[] { pipelineHook.Object });

                decoratedAggregateStore.Setup(mock => mock.Get(type, id)).Returns(aggregate.Object);

                Assert.Same(aggregate.Object, aggregateStore.Get(type, id));
            }
        }

        public class WhenSavingAggregate
        {
            [Fact]
            public void InvokePreSaveHooksBeforeDecoratedSave()
            {
                var aggregate = new Mock<Aggregate>();
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var aggregateStore = new HookableAggregateStore(decoratedAggregateStore.Object, new[] { pipelineHook.Object });


                // ReSharper disable AccessToDisposedClosure
                using (var context = new CommandContext(Guid.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                {
                    pipelineHook.Setup(mock => mock.PreSave(aggregate.Object, context)).Throws(new InvalidOperationException());

                    Assert.Throws<InvalidOperationException>(() => aggregateStore.Save(aggregate.Object, context));

                    decoratedAggregateStore.Verify(mock => mock.Save(aggregate.Object, context), Times.Never());
                }
                // ReSharper restore AccessToDisposedClosure
            }

            [Fact]
            public void InvokePostSaveHooksAfterDecoratedSave()
            {
                var aggregate = new Mock<Aggregate>();
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var commit = (Commit)FormatterServices.GetUninitializedObject(typeof(Commit));
                var aggregateStore = new HookableAggregateStore(decoratedAggregateStore.Object, new[] { pipelineHook.Object });


                // ReSharper disable AccessToDisposedClosure
                using (var context = new CommandContext(Guid.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                {
                    decoratedAggregateStore.Setup(mock => mock.Save(aggregate.Object, context)).Returns(new SaveResult(aggregate.Object, commit));
                    pipelineHook.Setup(mock => mock.PostSave(aggregate.Object, commit, null)).Throws(new InvalidOperationException());

                    Assert.Throws<InvalidOperationException>(() => aggregateStore.Save(aggregate.Object, context));

                    decoratedAggregateStore.Verify(mock => mock.Save(aggregate.Object, context), Times.Once());
                }
                // ReSharper restore AccessToDisposedClosure
            }

            [Fact]
            public void InvokePostSaveHooksIfDecoratedSaveThrowsException()
            {
                var aggregate = new Mock<Aggregate>();
                var pipelineHook = new Mock<PipelineHook>();
                var error = new InvalidOperationException();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var aggregateStore = new HookableAggregateStore(decoratedAggregateStore.Object, new[] { pipelineHook.Object });

                // ReSharper disable AccessToDisposedClosure
                using (var context = new CommandContext(Guid.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                {
                    decoratedAggregateStore.Setup(mock => mock.Save(aggregate.Object, context)).Throws(error);

                    Assert.Throws<InvalidOperationException>(() => aggregateStore.Save(aggregate.Object, context));

                    pipelineHook.Verify(mock => mock.PostSave(aggregate.Object, null, error), Times.Once());
                }
                // ReSharper restore AccessToDisposedClosure
            }

            [Fact]
            public void ReturnSaveResultIfNoExceptionsThrown()
            {
                var aggregate = new Mock<Aggregate>();
                var pipelineHook = new Mock<PipelineHook>();
                var decoratedAggregateStore = new Mock<IStoreAggregates>();
                var commit = (Commit)FormatterServices.GetUninitializedObject(typeof(Commit));
                var aggregateStore = new HookableAggregateStore(decoratedAggregateStore.Object, new[] { pipelineHook.Object });
                var saveResult = new SaveResult(aggregate.Object, commit);

                // ReSharper disable AccessToDisposedClosure
                using (var context = new CommandContext(Guid.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                {
                    decoratedAggregateStore.Setup(mock => mock.Save(aggregate.Object, context)).Returns(saveResult);

                    Assert.Same(saveResult, aggregateStore.Save(aggregate.Object, context));
                }
                // ReSharper restore AccessToDisposedClosure
            }
        }

        public class WhenDisposing
        {
            [Fact]
            public void CanCallDisposeMoreThanOnce()
            {
                var pipelineHook = new DisposablePipelineHook();
                var pipelineHooks = new PipelineHook[] { pipelineHook };
                var aggregateStore = new HookableAggregateStore(new Mock<IStoreAggregates>().Object, pipelineHooks);

                aggregateStore.Dispose();
                aggregateStore.Dispose();

                Assert.True(pipelineHook.Disposed);
            }

            private class DisposablePipelineHook : PipelineHook
            {
                public Boolean Disposed { get; private set; }

                public override void PreGet(Type aggregateType, Guid id)
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
