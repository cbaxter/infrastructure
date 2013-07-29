using System;
using System.Collections.Generic;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Mappings;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Data;
using Spark.Messaging;
using Test.Spark.Configuration;
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
    public static class UsingSagaEventHandler
    {
        public abstract class UsingSagaEventHandlerBase : IDisposable
        {
            protected readonly Mock<IPublishCommands> CommandPublisher = new Mock<IPublishCommands>();
            protected readonly Mock<IStoreSagas> SagaStore = new Mock<IStoreSagas>();
            protected readonly SagaEventHandler SagaEventHandler;
            protected readonly EventHandler EventHandler;
            protected readonly EventContext EventContext;
            protected readonly SagaMetadata SagaMetadata;
            protected readonly Guid AggregateId;
            protected readonly Guid SagaId;
            protected readonly Event Event;
            protected Boolean Handled;

            protected UsingSagaEventHandlerBase()
            {
                var executor = new Action<Object, Event>((handler, e) => { ((FakeSaga)handler).Handle((FakeEvent)e); Handled = true; });
                
                SagaId = GuidStrategy.NewGuid();
                AggregateId = GuidStrategy.NewGuid();
                Event = new FakeEvent { Id = SagaId };
                EventContext = new EventContext(AggregateId, HeaderCollection.Empty, Event);
                EventHandler = new EventHandler(typeof(FakeSaga), typeof(FakeEvent), executor, () => { throw new NotSupportedException(); });
                SagaMetadata = Saga.GetMetadata(typeof(FakeSaga), new HandleMethodCollection(new Dictionary<Type, Action<Object, Event>> { { typeof(FakeEvent), executor } }));
                SagaEventHandler = new SagaEventHandler(EventHandler, SagaMetadata, SagaStore.Object, CommandPublisher.Object);
            }

            public void Dispose()
            {
                EventContext.Dispose();
            }
        }

        public class WhenHandlingEvent : UsingSagaEventHandlerBase
        {
            [Fact]
            public void SagaIdExtractedFromEventConfiguration()
            {
                SagaEventHandler.Handle(EventContext);

                SagaStore.Verify(mock => mock.CreateSaga(typeof(FakeSaga), SagaId), Times.Once());
            }

            [Fact]
            public void PublishCommandsOnSuccessfulSave()
            {
                SagaStore.Setup(mock => mock.CreateSaga(typeof(FakeSaga), SagaId)).Returns(new FakeSaga());

                SagaEventHandler.Handle(EventContext);

                CommandPublisher.Verify(mock => mock.Publish(HeaderCollection.Empty, It.IsAny<CommandEnvelope>()), Times.Once());
            }

            [Fact]
            public void CommandsNotPublishedOnFailedSave()
            {
                var saga = new FakeSaga();

                SagaStore.Setup(mock => mock.CreateSaga(typeof(FakeSaga), SagaId)).Returns(saga);
                SagaStore.Setup(mock => mock.Save(saga, It.IsAny<SagaContext>())).Throws(new Exception());

                Assert.Throws<Exception>(() => SagaEventHandler.Handle(EventContext));

                CommandPublisher.Verify(mock => mock.Publish(HeaderCollection.Empty, It.IsAny<CommandEnvelope>()), Times.Never());
            }
        }

        private class FakeSaga : Saga
        {
            protected override void Configure(SagaConfiguration saga)
            {
                saga.CanStartWith((FakeEvent e) => e.Id);
            }

            public void Handle(FakeEvent e)
            {
                Publish(e.SourceId, new FakeCommand());
            }
        }

        private class FakeCommand : Command
        { }

        private class FakeEvent : Event
        {
            public Guid SourceId { get { return AggregateId; } }
            public Guid Id { get; set; }
        }
    }
}
