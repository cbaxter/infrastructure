using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Spark;
using Spark.Messaging;
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

namespace Test.Spark.Messaging
{
    public static class UsingMessageReceiver
    {
        public class WhenCreatingNewReceiver
        {
            [Fact]
            public void MessageReceiverCannotBeNull()
            {
                var messageProcessor = new Mock<IProcessMessages<Object>>();
                var ex = Assert.Throws<ArgumentNullException>(() => new MessageReceiver<Object>(null, messageProcessor.Object));

                Assert.Equal("messageReceiver", ex.ParamName);
            }

            [Fact]
            public void MessageProcessorCannotBeNull()
            {
                var messageReceiver = new Mock<IReceiveMessages<Object>>();
                var ex = Assert.Throws<ArgumentNullException>(() => new MessageReceiver<Object>(messageReceiver.Object, null));

                Assert.Equal("messageProcessor", ex.ParamName);
            }
        }

        public class WhenUsingDefaultTaskScheduler
        {
            [Fact]
            public void PassPayloadOnToCommandProcessor()
            {
                var messageProcessor = new FakeCommandProcessor();

                using (var messageBus = new BlockingCollectionMessageBus<Object>())
                using (new MessageReceiver<Object>(messageBus, messageProcessor))
                {
                    messageBus.Send(new Message<Object>(GuidStrategy.NewGuid(), HeaderCollection.Empty, new Object()));
                    messageBus.Dispose();
                }

                Assert.True(messageProcessor.WaitUntilProcessed());
            }

            [Fact]
            public void CanTolerateProcessorExceptions()
            {
                var message = new Message<Object>(GuidStrategy.NewGuid(), HeaderCollection.Empty, new Object());
                var messageProcessor = new Mock<IProcessMessages<Object>>();

                messageProcessor.Setup(mock => mock.ProcessAsync(message)).Throws(new InvalidOperationException());

                using (var messageBus = new BlockingCollectionMessageBus<Object>())
                using (new MessageReceiver<Object>(messageBus, messageProcessor.Object))
                {
                    messageBus.Send(message);
                    messageBus.Dispose();
                }
            }
        }

        private sealed class FakeCommandProcessor : IProcessMessages<Object>
        {
            private readonly ManualResetEvent commandProcessed = new ManualResetEvent(false);

            public Task ProcessAsync(Message<Object> message)
            {
                commandProcessed.Set();

                return Task.FromResult(default(Object));
            }

            public Boolean WaitUntilProcessed()
            {
                return commandProcessed.WaitOne(TimeSpan.FromMilliseconds(100));
            }
        }
    }
}
