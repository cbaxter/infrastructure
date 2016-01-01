using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
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
    namespace UsingOptimisticMessageSender
    {
        public class WhenSendingMessage
        {
            private readonly Mock<IProcessMessages<Object>> messageProcessor = new Mock<IProcessMessages<Object>>();

            [Fact]
            public void MessageCannotBeNull()
            {
                var bus = new OptimisticMessageSender<Object>(messageProcessor.Object);
                var ex = Assert.Throws<ArgumentNullException>(() => bus.Send(null));

                Assert.Equal("message", ex.ParamName);
            }

            [Fact]
            public void CannotSendIfDisposed()
            {
                var bus = new OptimisticMessageSender<Object>(messageProcessor.Object);

                bus.Dispose();

                Assert.Throws<ObjectDisposedException>(() => bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object())));
            }

            [Fact]
            public void BlockOnSendBlockedIfReachedBoundedCapacity()
            {
                var send1 = new ManualResetEvent(false);
                var send2 = new ManualResetEvent(false);
                var processor = new FakeMessageProcessor<Object>();
                var bus = new OptimisticMessageSender<Object>(processor, 1);

                Assert.Equal(1, bus.BoundedCapacity);

                Task.Run(() =>
                {
                    //NOTE: Once message is received for processing, the message is dequeued and thus we actually need
                    //      one more message to confirm blocking behavior (i.e., one in queue and one being processed).
                    bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));
                    bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));
                    send1.Set();
                });

                Task.Run(() =>
                {
                    send1.WaitOne();
                    bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));
                    send2.Set();
                });

                processor.WaitForMessage();

                Assert.True(send1.WaitOne(TimeSpan.FromMilliseconds(100)));
                Assert.False(send2.WaitOne(TimeSpan.FromMilliseconds(100)));

                processor.ProcessNextMessage();
                processor.WaitForMessage();

                Assert.True(send2.WaitOne(TimeSpan.FromMilliseconds(100)));
            }

            [Fact]
            public void WillRemoveFromQueueOnceMessageProcessed()
            {
                var processor = new FakeMessageProcessor<Object>();
                var bus = new OptimisticMessageSender<Object>(processor, 1);
                var attempt = 0;

                bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));
                processor.WaitForMessage();
                processor.ProcessNextMessage();

                while (bus.Count > 0 && attempt < 10) Thread.Sleep(100);

                Assert.Equal(0, bus.Count);
            }

            [Fact]
            public void WillRemoveFromQueueIfUnableToProcessMessage()
            {
                var processor = new FakeMessageProcessor<Object>();
                var bus = new OptimisticMessageSender<Object>(processor, 1);
                var ex = new InvalidOperationException();
                var attempt = 0;

                bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));
                processor.WaitForMessage();
                processor.ThrowException(ex);
                processor.ProcessNextMessage();

                while (bus.Count > 0 && attempt < 10) Thread.Sleep(100);

                Assert.Equal(0, bus.Count);
            }
        }

        public class WhenDisposing
        {
            [Fact]
            public void CanCallDisposeMultipleTimes()
            {
                var processor = new FakeMessageProcessor<Object>();
                using (var bus = new OptimisticMessageSender<Object>(processor))
                {
                    bus.Dispose();
                    bus.Dispose();
                }
            }

            [Fact]
            public void WillWaitForMessageDrain()
            {
                var processor = new FakeMessageProcessor<Object>();
                var bus = new OptimisticMessageSender<Object>(processor);
                var messages = new[]
                {
                    new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()),
                    new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()),
                    new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object())
                };

                foreach (var message in messages)
                    bus.Send(message);

                Task.Run(() =>
                {
                    foreach (var message in messages)
                    {
                        processor.WaitForMessage();
                        processor.ProcessNextMessage();
                    }
                });

                bus.Dispose();

                Assert.Equal(0, bus.Count);
            }

            [Fact]
            public void WillIgnorePosionMessageExceptions()
            {
                var processor = new FakeMessageProcessor<Object>();
                var bus = new OptimisticMessageSender<Object>(processor);
                var ex = new InvalidOperationException();

                bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));
                processor.WaitForMessage();
                processor.ThrowException(ex);
                processor.ProcessNextMessage();
                bus.Dispose();
            }
        }
    }
}
