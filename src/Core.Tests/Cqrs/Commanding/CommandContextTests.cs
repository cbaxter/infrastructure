using System;
using System.Threading;
using System.Threading.Tasks;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Eventing;
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

namespace Test.Spark.Cqrs.Commanding
{
    namespace UsingCommandContext
    {
        public class WhenCreatingNewContext
        {
            [Fact]
            public void HeaderCollectionCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandContext(Guid.NewGuid(), null, CommandEnvelope.Empty));

                Assert.Equal("headers", ex.ParamName);
            }

            [Fact]
            public void CommandEnvelopeCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandContext(Guid.NewGuid(), HeaderCollection.Empty, null));

                Assert.Equal("envelope", ex.ParamName);
            }

            [Fact]
            public void CurrentContextSetToNewCommandContextInstance()
            {
                var commandId = Guid.NewGuid();
                using (var context = new CommandContext(commandId, HeaderCollection.Empty, CommandEnvelope.Empty))
                {
                    Assert.Same(context, CommandContext.Current);
                    Assert.Equal(commandId, CommandContext.Current.CommandId);
                    Assert.Equal(HeaderCollection.Empty, CommandContext.Current.Headers);
                }
            }
        }

        public class WhenRaisingEvents
        {
            [Fact]
            public void RaiseMaintainsEventOrder()
            {
                FakeEvent e1 = new FakeEvent(), e2 = new FakeEvent(), e3 = new FakeEvent();

                using (var context = new CommandContext(Guid.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                {
                    context.Raise(e1);
                    context.Raise(e2);
                    context.Raise(e3);

                    var raisedEvents = context.GetRaisedEvents();
                    Assert.Equal(3, raisedEvents.Count);
                    Assert.Same(e1, raisedEvents[0]);
                    Assert.Same(e2, raisedEvents[1]);
                    Assert.Same(e3, raisedEvents[2]);
                }
            }

            private class FakeEvent : Event
            { }
        }

        public class WhenDisposing
        {
            [Fact]
            public void CannotDisposeContextFromAnotherThread()
            {
                var contextDisposedEvent = new ManualResetEvent(false);
                var contextCreatedEvent = new ManualResetEvent(false);
                var context = default(CommandContext);

                Task.Factory.StartNew(() =>
                    {
                        context = new CommandContext(Guid.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty);
                        contextCreatedEvent.Set();
                        contextDisposedEvent.WaitOne();
                        context.Dispose();
                    });

                contextCreatedEvent.WaitOne();

                var ex = Assert.Throws<InvalidOperationException>(() => context.Dispose());

                Assert.Equal(Exceptions.CommandContextInterleaved, ex.Message);

                contextDisposedEvent.Set();
            }

            [Fact]
            public void CannotDisposeContextOutOfOrder()
            {
                var context1 = new CommandContext(Guid.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty);
                var context2 = new CommandContext(Guid.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty);

                // ReSharper disable AccessToDisposedClosure
                var ex = Assert.Throws<InvalidOperationException>(() => context1.Dispose());
                // ReSharper restore AccessToDisposedClosure

                context2.Dispose();
                context1.Dispose();

                Assert.Equal(Exceptions.CommandContextInvalidThread, ex.Message);
            }

            [Fact]
            public void CanCallDisposeMoreThanOnce()
            {
                using (var context = new CommandContext(Guid.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                {
                    context.Dispose();
                    context.Dispose();
                }
            }
        }
    }
}
