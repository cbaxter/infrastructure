using System;
using System.Linq;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Messaging;
using Xunit;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Test.Spark.Cqrs.Commanding
{
    namespace UsingCommandPublisher
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
                var payload = new FakeCommand();

                publisher.Publish(GuidStrategy.NewGuid(), payload, null);

                messageFactory.Verify(mock => mock.Create(null, It.IsAny<CommandEnvelope>()), Times.Once);
                messageBus.Verify(mock => mock.Send(It.IsAny<Message<CommandEnvelope>>()), Times.Once);
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

            [Fact]
            public void CanPublishMultipleCommandsToSameAggregate()
            {
                var aggregateId = GuidStrategy.NewGuid();
                var publisher = new Mock<IPublishCommands>();
                var commands = new[] { new FakeCommand(), new FakeCommand() };

                publisher.Object.Publish(aggregateId, commands);

                publisher.Verify(mock => mock.Publish(null, It.IsAny<CommandEnvelope>()), Times.Exactly(2));
            }
        }

        internal class FakeCommand : Command
        { }
    }
}
