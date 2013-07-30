using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Messaging;
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
    public static class UsingSagaContext
    {
        public class WhenCreatingNewContext
        {
            [Fact]
            public void SagaTypeCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new SagaContext(null, GuidStrategy.NewGuid(), new FakeEvent()));

                Assert.Equal("sagaType", ex.ParamName);
            }

            [Fact]
            public void EventCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), null));

                Assert.Equal("e", ex.ParamName);
            }

            [Fact]
            public void CurrentContextSetToNewSagaContextInstance()
            {
                var e = new FakeEvent();
                var sagaId = GuidStrategy.NewGuid();
                using (var context = new SagaContext(typeof(Saga), sagaId, e))
                {
                    Assert.Same(context, SagaContext.Current);
                    Assert.Equal(sagaId, SagaContext.Current.SagaId);
                    Assert.Equal(typeof(Saga), SagaContext.Current.SagaType);
                    Assert.Equal(e, SagaContext.Current.Event);
                }
            }
        }

        public class WhenPublishingCommands
        {
            [Fact]
            public void CollectCommandsToBePublished()
            {
                var sagaCommand1 = new SagaCommand(GuidStrategy.NewGuid(), new HeaderCollection((IEnumerable<Header>) HeaderCollection.Empty), new FakeCommand());
                var sagaCommand2 = new SagaCommand(GuidStrategy.NewGuid(), new HeaderCollection((IEnumerable<Header>)HeaderCollection.Empty), new FakeCommand());

                using (var context = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), new FakeEvent()))
                {
                    context.Publish(sagaCommand1.AggregateId, sagaCommand1.Headers, sagaCommand1.Command);
                    context.Publish(sagaCommand2.AggregateId, sagaCommand2.Headers, sagaCommand2.Command);

                    var publishedCommands = context.GetPublishedCommands().ToArray();
                    Assert.Equal(sagaCommand1.AggregateId, publishedCommands[0].AggregateId);
                    Assert.Equal(sagaCommand2.AggregateId, publishedCommands[1].AggregateId);
                }
            }


            public sealed class FakeCommand : Command
            { }
        }

        public class WhenDisposing
        {
            [Fact]
            public void CannotDisposeContextFromAnotherThread()
            {
                var contextDisposedEvent = new ManualResetEvent(false);
                var contextCreatedEvent = new ManualResetEvent(false);
                var context = default(SagaContext);

                Task.Factory.StartNew(() =>
                    {
                        context = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), new FakeEvent());
                        contextCreatedEvent.Set();
                        contextDisposedEvent.WaitOne();
                        context.Dispose();
                    });

                contextCreatedEvent.WaitOne();

                var ex = Assert.Throws<InvalidOperationException>(() => context.Dispose());

                Assert.Equal(Exceptions.SagaContextInterleaved, ex.Message);

                contextDisposedEvent.Set();
            }

            [Fact]
            public void CannotDisposeContextOutOfOrder()
            {
                var context1 = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), new FakeEvent());
                var context2 = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), new FakeEvent());

                // ReSharper disable AccessToDisposedClosure
                var ex = Assert.Throws<InvalidOperationException>(() => context1.Dispose());
                // ReSharper restore AccessToDisposedClosure

                context2.Dispose();
                context1.Dispose();

                Assert.Equal(Exceptions.SagaContextInvalidThread, ex.Message);
            }

            [Fact]
            public void CanCallDisposeMoreThanOnce()
            {
                using (var context = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), new FakeEvent()))
                {
                    context.Dispose();
                    context.Dispose();
                }
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnFriendlyDescription()
            {
                var sagaType = typeof(Saga);
                var sagaId = GuidStrategy.NewGuid();

                using (var context = new SagaContext(sagaType, sagaId, new FakeEvent()))
                    Assert.Equal(String.Format("{0} - {1}", sagaType, sagaId), context.ToString());
            }
        }

        public class FakeEvent : Event
        { }
    }
}
