using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Spark;
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

namespace Test.Spark.Messaging
{
    namespace UsingDirectMessageSender
    {
        public class WhenSendingMessages
        {
            [Fact]
            public void BlockUntilMessageProcessed()
            {
                var messageProcessor = new FakeMessageProcessor();
                var messageBus = new DirectMessageSender<Object>(messageProcessor);
                var message = new Message<Object>(GuidStrategy.NewGuid(), HeaderCollection.Empty, new Object());

                messageBus.Send(message);

                Assert.True(messageProcessor.Processed);
            }

            [Fact]
            public void UnwwrapAggregateExceptionOnError()
            {
                var messageProcessor = new Mock<IProcessMessages<Object>>();
                var messageBus = new DirectMessageSender<Object>(messageProcessor.Object);
                var message = new Message<Object>(GuidStrategy.NewGuid(), HeaderCollection.Empty, new Object());

                messageProcessor.Setup(mock => mock.Process(message)).Throws(new AggregateException(new InvalidOperationException()));

                Assert.Throws<InvalidOperationException>(() => messageBus.Send(message));
            }

            protected sealed class FakeMessageProcessor : IProcessMessages<Object>, IDisposable
            {
                private readonly ManualResetEvent processed = new ManualResetEvent(initialState: false);

                public Boolean Processed { get; private set; }

                public void Dispose()
                {
                    processed.Dispose();
                }

                public void Process(Message<Object> message)
                {
                    Processed = true;
                    processed.Set();
                }

                public Task ProcessAsync(Message<Object> message)
                {
                    return Task.Factory.StartNew(() =>
                        {
                            Thread.Sleep(100);
                            Processed = true;
                            processed.Set();
                        });
                }

                public void WaitUntilProcessed()
                {
                    processed.WaitOne();
                }
            }
        }
    }
}
