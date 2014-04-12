using System;
using JetBrains.Annotations;
using Spark;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Resources;
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
    namespace UsingSagaMetadata
    {
        public class WhenCheckingCanStartWithEventType
        {
            [Fact]
            public void ReturnTrueIfSagaCanStartWithEventType()
            {
                var configuration = new SagaConfiguration(typeof(FakeSaga));

                configuration.CanStartWith((FakeInitiatingEvent e) => e.Id);

                Assert.True(configuration.GetMetadata().CanStartWith(typeof(FakeInitiatingEvent)));
            }

            [Fact]
            public void ReturnFalseIfSagaCannotStartWithEventType()
            {
                var configuration = new SagaConfiguration(typeof(FakeSaga));

                configuration.CanStartWith((FakeInitiatingEvent e) => e.Id);

                Assert.False(configuration.GetMetadata().CanStartWith(typeof(FakeUnhandledEvent)));
            }
        }

        public class WhenCheckingCanHandleEventType
        {
            [Fact]
            public void ReturnTrueIfSagaCanStartWithEventType()
            {
                var configuration = new SagaConfiguration(typeof(FakeSaga));

                configuration.CanHandle((FakeHandledEvent e) => e.Id);

                Assert.True(configuration.GetMetadata().CanHandle(typeof(FakeHandledEvent)));
            }

            [Fact]
            public void ReturnFalseIfSagaCannotStartWithEventType()
            {
                var configuration = new SagaConfiguration(typeof(FakeSaga));

                configuration.CanHandle((FakeHandledEvent e) => e.Id);

                Assert.False(configuration.GetMetadata().CanHandle(typeof(FakeUnhandledEvent)));
            }
        }

        public class WhenGettingEventCorrelationId
        {
            private readonly SagaMetadata sagaMetadata;

            public WhenGettingEventCorrelationId()
            {
                var configuration = new SagaConfiguration(typeof(FakeSaga));

                configuration.CanStartWith((FakeInitiatingEvent e) => e.Id);
                configuration.CanHandle((FakeHandledEvent e) => e.Id);

                sagaMetadata = configuration.GetMetadata();
            }

            [Fact]
            public void EventCannotBeNull()
            {
                Assert.Throws<ArgumentNullException>(() => sagaMetadata.GetCorrelationId((default(Event))));
            }

            [Fact]
            public void ResolveCorrelationIdForKnownEvents()
            {
                var correlationId = GuidStrategy.NewGuid();

                Assert.Equal(correlationId, sagaMetadata.GetCorrelationId(new FakeHandledEvent { Id = correlationId }));
            }

            [Fact]
            public void ThrowInvalidOperationExceptionForUnknownEvents()
            {
                var ex = Assert.Throws<InvalidOperationException>(()=> sagaMetadata.GetCorrelationId(new FakeUnhandledEvent { Id = GuidStrategy.NewGuid() }));

                Assert.Equal(Exceptions.EventTypeNotConfigured.FormatWith(typeof(FakeSaga), typeof(FakeUnhandledEvent)), ex.Message);
            }
        }

        internal sealed class FakeSaga : Saga
        {
            protected override void Configure(SagaConfiguration saga)
            {
                saga.CanStartWith(((FakeInitiatingEvent e) => e.Id));
                saga.CanHandle(((FakeHandledEvent e) => e.Id));
            }

            [UsedImplicitly]
            public void Handle(FakeInitiatingEvent e)
            { }

            [UsedImplicitly]
            public void Handle(FakeHandledEvent e)
            { }
        }

        internal sealed class FakeInitiatingEvent : Event
        {
            public Guid Id { get; set; }
        }

        internal sealed class FakeHandledEvent : Event
        {
            public Guid Id { get; set; }
        }

        internal sealed class FakeUnhandledEvent : Event
        {
            public Guid Id { get; set; }
        }
    }
}
