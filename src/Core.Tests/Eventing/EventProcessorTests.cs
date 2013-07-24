using System;
using JetBrains.Annotations;
using Moq;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Messaging;
using Xunit;
using EventHandler = Spark.Infrastructure.Eventing.EventHandler;

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

namespace Spark.Infrastructure.Tests.Eventing
{
    public static class UsingEventProcessor
    {
        public abstract class UsingEventProcessorBase
        {
            protected readonly Mock<IRetrieveEventHandlers> HandlerRegistry = new Mock<IRetrieveEventHandlers>();
            protected readonly Mock<IProcessEventSettings> Settings = new Mock<IProcessEventSettings>();
            protected readonly Mock<IStoreAggregates> AggregateStore = new Mock<IStoreAggregates>();
            protected readonly EventProcessor Processor;

            protected UsingEventProcessorBase()
            {
                Settings.Setup(mock => mock.BoundedCapacity).Returns(100);
                Settings.Setup(mock => mock.MaximumConcurrencyLevel).Returns(10);
                Settings.Setup(mock => mock.RetryTimeout).Returns(TimeSpan.FromSeconds(10));

                Processor = new EventProcessor(HandlerRegistry.Object, Settings.Object);
            }
        }

        public class WhenCreatingNewProcessor
        {
            [Fact]
            public void EventHandlerRegistryCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new EventProcessor(null));

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
        }

        private class FakeEventHandler : EventHandler
        {
            public Boolean Handled { get; private set; }

            public FakeEventHandler(Type handlerType, Type eventType, Action<Object, Event> executor, Func<Object> eventHandlerFactory)
                : base(handlerType, eventType, executor, eventHandlerFactory)
            { }

            public override void Handle(EventContext context, TimeSpan retryTimeout)
            {
                Handled = true;
            }
        }

        private class FakeAggregate : Aggregate
        {
            [UsedImplicitly]
            public void Handle(FakeEvent command)
            { }
        }

        private class FakeEvent : Event
        { }
    }
}
