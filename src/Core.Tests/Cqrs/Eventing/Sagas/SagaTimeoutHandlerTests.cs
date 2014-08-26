using System;
using JetBrains.Annotations;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Messaging;
using Xunit;
using EventHandler = Spark.Cqrs.Eventing.EventHandler;

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
    namespace UsingSagaTimeoutHandler
    {
        public abstract class UsingSagaEventHandlerBase
        {
            protected readonly Mock<IStoreSagas> SagaStore = new Mock<IStoreSagas>();
            protected readonly SagaEventHandler SagaEventHandler;
            protected Boolean Handled;

            protected UsingSagaEventHandlerBase()
            {
                var sagaMetadata = new FakeSaga().GetMetadata();
                var commandPublisher = new Mock<IPublishCommands>();
                var executor = new Action<Object, Event>((handler, e) => { ((FakeSaga)handler).Handle((Timeout)e); Handled = true; });
                var eventHandler = new EventHandler(typeof(FakeSaga), typeof(Timeout), executor, () => { throw new NotSupportedException(); });
                
                SystemTime.ClearOverride();
                SagaEventHandler = new SagaTimeoutHandler(eventHandler, sagaMetadata, SagaStore.Object, new Lazy<IPublishCommands>(() => commandPublisher.Object));
            }

            protected Saga ConfigureSagaTimeout(DateTime? timeout)
            {
                Saga saga = new FakeSaga();

                saga.Timeout = timeout;
                saga.CorrelationId = GuidStrategy.NewGuid();
                SagaStore.Setup(mock => mock.TryGetSaga(saga.GetType(), saga.CorrelationId, out saga)).Returns(true);

                return saga;
            }

            protected EventContext CreateEventContext(Saga saga, DateTime timeout)
            {
                return new EventContext(saga.CorrelationId, HeaderCollection.Empty, new Timeout(saga.GetType(), timeout));
            }
        }

        public class WhenHandlingTimeoutEvent : UsingSagaEventHandlerBase
        {
            [Fact]
            public void ClearTimeoutIfTimeoutScheduledAndMatchesExpectedTimeout()
            {
                var timeout = SystemTime.Now;
                var saga = ConfigureSagaTimeout(timeout);

                using (var eventContext = CreateEventContext(saga, timeout))
                {
                    SagaEventHandler.Handle(eventContext);

                    SagaStore.Verify(mock => mock.Save(saga, It.Is((SagaContext context) => context.TimeoutChanged)), Times.Once);
                }

                Assert.Null(saga.Timeout);
                Assert.True(Handled);
            }

            [Fact]
            public void IgnoreTimeoutIfNoTimeoutScheduled()
            {
                var timeout = SystemTime.Now;
                var saga = ConfigureSagaTimeout(default(DateTime?));

                using (var eventContext = CreateEventContext(saga, timeout))
                {
                    SagaEventHandler.Handle(eventContext);

                    SagaStore.Verify(mock => mock.Save(saga, It.Is((SagaContext context) => !context.TimeoutChanged)), Times.Once);
                }

                Assert.Null(saga.Timeout);
                Assert.False(Handled);
            }

            [Fact]
            public void IgnoreTimeoutIfTimeoutDoesNotMatchScheduled()
            {
                var timeout = SystemTime.Now.Subtract(TimeSpan.FromSeconds(1));
                var saga = ConfigureSagaTimeout(timeout);

                using (var eventContext = CreateEventContext(saga, SystemTime.Now))
                {
                    SagaEventHandler.Handle(eventContext);

                    SagaStore.Verify(mock => mock.Save(saga, It.Is((SagaContext context) => !context.TimeoutChanged)), Times.Once);
                }

                Assert.Equal(timeout, saga.Timeout);
                Assert.False(Handled);
            }
        }

        internal class FakeSaga : Saga
        {
            protected override void Configure(SagaConfiguration saga)
            {
                saga.CanStartWith((FakeEvent e) => e.Id);
                saga.CanHandle((Timeout e) => e.CorrelationId);
            }

            [UsedImplicitly]
            public void Handle(Timeout e)
            { }

            [UsedImplicitly]
            public void Handle(FakeEvent e)
            { }
        }

        internal class FakeEvent : Event
        {
            public Guid SourceId { get { return AggregateId; } }
            public Guid Id { get; set; }
        }
    }
}
