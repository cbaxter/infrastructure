using System;
using JetBrains.Annotations;
using Moq;
using Spark;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing;
using Spark.Data;
using Spark.Messaging;
using Test.Spark.Configuration;
using Xunit;
using EventHandler = Spark.Cqrs.Eventing.EventHandler;

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

namespace Test.Spark.Cqrs.Eventing
{
    namespace UsingEventProcessor
    {
        public abstract class UsingEventProcessorBase
        {
            internal readonly Mock<IDetectTransientErrors> TransientErrorRegistry = new Mock<IDetectTransientErrors>();
            internal readonly Mock<IRetrieveEventHandlers> HandlerRegistry = new Mock<IRetrieveEventHandlers>();
            internal readonly Mock<IStoreAggregates> AggregateStore = new Mock<IStoreAggregates>();
            internal readonly EventProcessor Processor;

            protected UsingEventProcessorBase()
            {
                TransientErrorRegistry.Setup(mock => mock.IsTransient(It.IsAny<ConcurrencyException>())).Returns(true);
                Processor = new EventProcessor(HandlerRegistry.Object, TransientErrorRegistry.Object, new EventProcessorSettings());
            }
        }

        public class WhenCreatingNewProcessor
        {
            [Fact]
            public void EventHandlerRegistryCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new EventProcessor(null, new IDetectTransientErrors[0]));

                Assert.Equal("eventHandlerRegistry", ex.ParamName);
            }
        }

        public class WhenProcessingEvents : UsingEventProcessorBase
        {
            [Fact]
            public void RetrieveEventHandlersBasedOnEventType()
            {
                var e = new FakeEvent();
                var eventHandler1 = new FakeEventHandler(typeof(Object), typeof(FakeEvent), (a, b) => { }, () => new Object());
                var eventHandler2 = new FakeEventHandler(typeof(Object), typeof(FakeEvent), (a, b) => { }, () => new Object());
                var envelope = new EventEnvelope(GuidStrategy.NewGuid(), GuidStrategy.NewGuid(), new EventVersion(1, 1, 1), e);
                var message = Message.Create(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope);

                HandlerRegistry.Setup(mock => mock.GetHandlersFor(e)).Returns(new EventHandler[] { eventHandler1, eventHandler2 });

                Processor.Process(message);

                Assert.True(eventHandler1.Handled);
                Assert.True(eventHandler2.Handled);
            }

            [Fact]
            public void CanTolerateTransientExceptions()
            {
                var execution = 0;
                var e = new FakeEvent();
                var eventHandler = new FakeEventHandler(typeof(Object), typeof(FakeEvent), (a, b) => { if (++execution == 1) { throw new ConcurrencyException(); } }, () => new Object());
                var envelope = new EventEnvelope(GuidStrategy.NewGuid(), GuidStrategy.NewGuid(), new EventVersion(1, 1, 1), e);
                var message = Message.Create(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope);

                HandlerRegistry.Setup(mock => mock.GetHandlersFor(e)).Returns(new EventHandler[] { eventHandler });

                Processor.Process(message);
            }

            [Fact]
            public void WillTimeoutEventuallyIfCannotExecuteHandler()
            {
                var e = new FakeEvent();
                var eventHandler = new FakeEventHandler(typeof(Object), typeof(FakeEvent), (a, b) => { throw new ConcurrencyException(); }, () => new Object());
                var envelope = new EventEnvelope(GuidStrategy.NewGuid(), GuidStrategy.NewGuid(), new EventVersion(1, 1, 1), e);
                var message = Message.Create(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope);

                SystemTime.ClearOverride();

                HandlerRegistry.Setup(mock => mock.GetHandlersFor(e)).Returns(new EventHandler[] { eventHandler });

                Assert.Throws<TimeoutException>(() => Processor.Process(message));
            }
        }

        internal class FakeEventHandler : EventHandler
        {
            public Boolean Handled { get; private set; }

            public FakeEventHandler(Type handlerType, Type eventType, Action<Object, Event> executor, Func<Object> eventHandlerFactory)
                : base(handlerType, eventType, executor, eventHandlerFactory)
            { }

            public override void Handle(EventContext context)
            {
                Executor(this, context.Event);
                Handled = true;
            }
        }

        internal class FakeAggregate : Aggregate
        {
            [UsedImplicitly]
            public void Handle(FakeEvent command)
            { }
        }

        internal class FakeEvent : Event
        { }
    }
}
