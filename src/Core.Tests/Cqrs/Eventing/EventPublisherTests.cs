using System;
using Moq;
using Spark.Cqrs.Eventing;
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

namespace Test.Spark.Cqrs.Eventing
{
    namespace UsingEventPublisher
    {
        public class WhenCreatingNewPublisher
        {
            [Fact]
            public void MessageFactoryCannotBeNull()
            {
                var messageBus = new Mock<ISendMessages<EventEnvelope>>();
                var ex = Assert.Throws<ArgumentNullException>(() => new EventPublisher(null, messageBus.Object));

                Assert.Equal("messageFactory", ex.ParamName);
            }

            [Fact]
            public void MessageSenderCannotBeNull()
            {
                var messageFactory = new Mock<ICreateMessages>();
                var ex = Assert.Throws<ArgumentNullException>(() => new EventPublisher(messageFactory.Object, null));

                Assert.Equal("messageSender", ex.ParamName);
            }
        }

        public class WhenPublishingEvents
        {
            [Fact]
            public void EventEnvelopeCannotBeNull()
            {
                var messageFactory = new Mock<ICreateMessages>();
                var messageBus = new Mock<ISendMessages<EventEnvelope>>();
                var publisher = new EventPublisher(messageFactory.Object, messageBus.Object);

                var ex = Assert.Throws<ArgumentNullException>(() => publisher.Publish(HeaderCollection.Empty, null));

                Assert.Equal("payload", ex.ParamName);
            }

            [Fact]
            public void HeadersCanBeNull()
            {
                var messageFactory = new Mock<ICreateMessages>();
                var messageBus = new Mock<ISendMessages<EventEnvelope>>();
                var publisher = new EventPublisher(messageFactory.Object, messageBus.Object);

                publisher.Publish(null, EventEnvelope.Empty);

                messageFactory.Verify(mock => mock.Create(null, EventEnvelope.Empty), Times.Once);
                messageBus.Verify(mock => mock.Send(It.IsAny<Message<EventEnvelope>>()), Times.Once);
            }
        }
    }
}
