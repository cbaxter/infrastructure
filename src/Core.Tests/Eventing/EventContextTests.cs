using System;
using System.Threading;
using System.Threading.Tasks;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Messaging;
using Spark.Infrastructure.Resources;
using Xunit;

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
    public static class UsingEventContext
    {
        public class WhenCreatingNewContext
        {
            [Fact]
            public void HeaderCollectionCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new EventContext(Guid.NewGuid(), null, new FakeEvent()));

                Assert.Equal("headers", ex.ParamName);
            }

            [Fact]
            public void EventCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new EventContext(Guid.NewGuid(), HeaderCollection.Empty, null));

                Assert.Equal("e", ex.ParamName);
            }

            [Fact]
            public void CurrentContextSetToNewEventContextInstance()
            {
                var @event = new FakeEvent();
                var aggregateId = Guid.NewGuid();

                using (var context = new EventContext(aggregateId, HeaderCollection.Empty, @event))
                {
                    Assert.Same(context, EventContext.Current);
                    Assert.Same(@event, EventContext.Current.Event);
                    Assert.Equal(aggregateId, EventContext.Current.AggregateId);
                    Assert.Equal(HeaderCollection.Empty, EventContext.Current.Headers);
                }
            }

            protected sealed class FakeEvent : Event
            { }
        }

        public class WhenDisposing
        {
            [Fact]
            public void CannotDisposeEventFromAnotherThread()
            {
                var contextDisposedEvent = new ManualResetEvent(false);
                var contextCreatedEvent = new ManualResetEvent(false);
                var context = default(EventContext);

                Task.Factory.StartNew(() =>
                    {
                        context = new EventContext(Guid.NewGuid(), HeaderCollection.Empty, new FakeEvent());
                        contextCreatedEvent.Set();
                        contextDisposedEvent.WaitOne();
                        context.Dispose();
                    });

                contextCreatedEvent.WaitOne();

                var ex = Assert.Throws<InvalidOperationException>(() => context.Dispose());

                Assert.Equal(Exceptions.EventContextInterleaved, ex.Message);

                contextDisposedEvent.Set();
            }

            [Fact]
            public void CannotDisposeContextOutOfOrder()
            {
                var context1 = new EventContext(Guid.NewGuid(), HeaderCollection.Empty, new FakeEvent());
                var context2 = new EventContext(Guid.NewGuid(), HeaderCollection.Empty, new FakeEvent());

                // ReSharper disable AccessToDisposedClosure
                var ex = Assert.Throws<InvalidOperationException>(() => context1.Dispose());
                // ReSharper restore AccessToDisposedClosure

                context2.Dispose();
                context1.Dispose();

                Assert.Equal(Exceptions.EventContextInvalidThread, ex.Message);
            }

            [Fact]
            public void CanCallDisposeMoreThanOnce()
            {
                using (var context = new EventContext(Guid.NewGuid(), HeaderCollection.Empty, new FakeEvent()))
                {
                    context.Dispose();
                    context.Dispose();
                }
            }

            protected sealed class FakeEvent : Event
            { }
        }
    }
}
