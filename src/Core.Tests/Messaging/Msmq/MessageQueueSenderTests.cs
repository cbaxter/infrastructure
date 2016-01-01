using System;
using Spark.Messaging;
using Spark.Messaging.Msmq;
using Spark.Serialization;
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

namespace Test.Spark.Messaging.Msmq
{
    namespace UsingMessageQueueSender
    {
        public class WhenSendingMessages : IDisposable
        {
            private readonly ISendMessages<String> messageSender;
            private readonly ISerializeObjects serializer;
            private readonly MessageQueue testQueue;

            public WhenSendingMessages()
            {
                serializer = new BinarySerializer();
                messageSender = new MessageSender<String>(TestMessageQueue.Path, serializer);
                testQueue = TestMessageQueue.Create();
                testQueue.EnsureQueueExists();
                testQueue.Purge();
            }

            public void Dispose()
            {
                TestMessageQueue.Purge();
            }

            [MessageQueueFact]
            public void WillQueueMessageForProcessing()
            {
                var message = new Message<String>(Guid.NewGuid(), HeaderCollection.Empty, "Test Message");

                messageSender.Send(message);

                Assert.Equal(message.Id, ReceiveMessage().Id);
            }

            private Message<String> ReceiveMessage()
            {
                var message = testQueue.Receive(TimeSpan.FromSeconds(1));

                Assert.NotNull(message);

                return serializer.Deserialize<Message<String>>(message.BodyStream);
            }
        }
    }
}
