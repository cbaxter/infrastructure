using System;
using System.Linq;
using Moq;
using Spark.Cqrs.Commanding;
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

namespace Spark.Tests.Cqrs.Commanding
{
    public static class UsingCommandPublisher
    {
        public class WhenCreatingNewPublisher
        {
            [Fact]
            public void MessageFactoryCannotBeNull()
            {
                var messageBus = new Mock<ISendMessages<CommandEnvelope>>();
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandPublisher(null, messageBus.Object));

                Assert.Equal("messageFactory", ex.ParamName);
            }

            [Fact]
            public void MessageSenderCannotBeNull()
            {
                var messageFactory = new Mock<ICreateMessages>();
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandPublisher(messageFactory.Object, null));

                Assert.Equal("messageSender", ex.ParamName);
            }
        }

        public class WhenPublishingCommand
        {
            [Fact]
            public void CommandCannotBeNull()
            {
                var messageFactory = new Mock<ICreateMessages>();
                var messageBus = new Mock<ISendMessages<CommandEnvelope>>();
                var publisher = new CommandPublisher(messageFactory.Object, messageBus.Object);

                var ex = Assert.Throws<ArgumentNullException>(() => publisher.Publish(GuidStrategy.NewGuid(), null, HeaderCollection.Empty));

                Assert.Equal("command", ex.ParamName);
            }

            [Fact]
            public void HeadersCanBeNull()
            {
                var messageFactory = new Mock<ICreateMessages>();
                var messageBus = new Mock<ISendMessages<CommandEnvelope>>();
                var publisher = new CommandPublisher(messageFactory.Object, messageBus.Object);

                Assert.DoesNotThrow(() => publisher.Publish(GuidStrategy.NewGuid(), new FakeCommand(), null));
            }

            [Fact]
            public void WrapCommandInMessageEnvelope()
            {
                var command = new FakeCommand() as Command;
                var messageFactory = new Mock<ICreateMessages>();
                var messageBus = new Mock<ISendMessages<CommandEnvelope>>();
                var publisher = new CommandPublisher(messageFactory.Object, messageBus.Object);
                var message = new Message<CommandEnvelope>(GuidStrategy.NewGuid(), HeaderCollection.Empty, new CommandEnvelope(GuidStrategy.NewGuid(), command));

                messageFactory.Setup(mock => mock.Create(HeaderCollection.Empty, It.Is<CommandEnvelope>(envelope => ReferenceEquals(command, envelope.Command)))).Returns(message);

                publisher.Publish(GuidStrategy.NewGuid(), command, HeaderCollection.Empty);

                messageBus.Verify(mock => mock.Send(message), Times.Once());
            }
        }

        public class WhenUsingExtendedPublishMethods
        {
            [Fact]
            public void UseNullHeaderCollectionIfOnlyCommandSpecified()
            {
                var command = new FakeCommand();
                var aggregateId = GuidStrategy.NewGuid();
                var publisher = new Mock<IPublishCommands>();

                publisher.Object.Publish(aggregateId, command);

                publisher.Verify(mock => mock.Publish(null, It.IsAny<CommandEnvelope>()), Times.Once());
            }

            [Fact]
            public void UseParamsOverloadWhenAvailable()
            {
                var command = new FakeCommand();
                var aggregateId = GuidStrategy.NewGuid();
                var publisher = new Mock<IPublishCommands>();
                var header = new Header("MyHeader", "MyValue");

                publisher.Object.Publish(aggregateId, command, header);

                publisher.Verify(mock => mock.Publish(It.Is<Header[]>(headers => Equals(headers.Single(), header)), It.IsAny<CommandEnvelope>()), Times.Once());
            }

            [Fact]
            public void UseHeadersOverloadWhenAvailable()
            {
                var command = new FakeCommand();
                var aggregateId = GuidStrategy.NewGuid();
                var publisher = new Mock<IPublishCommands>();
                var header = new Header("MyHeader", "MyValue");

                publisher.Object.Publish(aggregateId, command, new[] { header });

                publisher.Verify(mock => mock.Publish(It.Is<Header[]>(headers => Equals(headers.Single(), header)), It.IsAny<CommandEnvelope>()), Times.Once());
            }
        }

        private class FakeCommand : Command
        { }
    }
}
