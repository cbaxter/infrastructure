using System;
using System.Threading;
using Moq;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Messaging;
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

namespace Spark.Infrastructure.Tests.Commanding
{
    public static class UsingCommandReceiver
    {
        public class WhenCreatingNewReceiver
        {
            [Fact]
            public void MessageReceiverCannotBeNull()
            {
                var commandProcessor = new Mock<IProcessCommands>();
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandReceiver(null, commandProcessor.Object));

                Assert.Equal("messageReceiver", ex.ParamName);
            }

            [Fact]
            public void CommandProcessorCannotBeNull()
            {
                var messageReceiver = new Mock<IReceiveMessages<Command>>();
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandReceiver(messageReceiver.Object, null));

                Assert.Equal("commandProcessor", ex.ParamName);
            }
        }

        public class WhenUsingDefaultTaskScheduler
        {
            [Fact]
            public void CanTolerateNullCommandPayload()
            {
                var commandProcessor = new Mock<IProcessCommands>(); ;

                using (var messageBus = new BlockingCollectionMessageBus<Command>())
                using (new CommandReceiver(messageBus, commandProcessor.Object))
                {
                    messageBus.Send(new Message<Command>(Guid.NewGuid(), HeaderCollection.Empty, null));
                    messageBus.Dispose();
                }
            }

            [Fact]
            public void PassPayloadOnToCommandProcessor()
            {
                var commandProcessor = new FakeCommandProcessor();

                using (var messageBus = new BlockingCollectionMessageBus<Command>())
                using (new CommandReceiver(messageBus, commandProcessor))
                {
                    messageBus.Send(new Message<Command>(Guid.NewGuid(), HeaderCollection.Empty, new FakeCommand()));
                    messageBus.Dispose();
                }

                Assert.True(commandProcessor.WaitUntilProcessed());
            }

            [Fact]
            public void CanTolerateProcessorExceptions()
            {
                var message = new Message<Command>(Guid.NewGuid(), HeaderCollection.Empty, new FakeCommand());
                var commandProcessor = new Mock<IProcessCommands>();

                commandProcessor.Setup(mock => mock.Process(message.Id, message.Headers, message.Payload)).Throws(new InvalidOperationException());

                using (var messageBus = new BlockingCollectionMessageBus<Command>())
                using (new CommandReceiver(messageBus, commandProcessor.Object))
                {
                    messageBus.Send(message);
                    messageBus.Dispose();
                }
            }
        }

        private sealed class FakeCommand : Command
        {
            protected override Guid GetAggregateId()
            {
                return Guid.NewGuid();
            }
        }

        private sealed class FakeCommandProcessor : IProcessCommands
        {
            private readonly ManualResetEvent commandProcessed = new ManualResetEvent(false);

            public void Process(Guid commandId, HeaderCollection headers, Command command)
            {
                commandProcessed.Set();
            }

            public Boolean WaitUntilProcessed()
            {
                return commandProcessed.WaitOne(TimeSpan.FromMilliseconds(100));
            }
        }
    }
}
