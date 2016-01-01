using System;
using System.Linq;
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
    namespace UsingMessageQueueReceiver
    {
        public class WhenInitializingMessageReceiver : IDisposable
        {
            private readonly FakeMessageProcessor<String> messageProcessor;
            private readonly MessageQueue processingQueue;
            private readonly MessageQueue testQueue;

            public WhenInitializingMessageReceiver()
            {
                messageProcessor = new FakeMessageProcessor<String>();
                processingQueue = TestMessageQueue.Create("processing");
                testQueue = TestMessageQueue.Create();
                testQueue.EnsureQueueExists();
                testQueue.Purge();
            }

            public void Dispose()
            {
                TestMessageQueue.Purge();
            }

            [MessageQueueFact]
            public void WillEmptyProcessingQueueBeforeProcessingPendingMessages()
            {
                testQueue.SendMessage("Message #1");
                testQueue.Move(testQueue.SendMessage("Message #2"), processingQueue);

                using (new MessageReceiver<String>(TestMessageQueue.Path, new BinarySerializer(), messageProcessor))
                    messageProcessor.ProcessMessages(count: 2);

                Assert.Equal("Message #2", messageProcessor.Messages.First().Payload);
                Assert.Equal("Message #1", messageProcessor.Messages.Last().Payload);
            }
        }

        public class WhenReceivingMessages : IDisposable
        {
            private readonly FakeMessageProcessor<String> messageProcessor;
            private readonly MessageQueue testQueue;

            public WhenReceivingMessages()
            {
                messageProcessor = new FakeMessageProcessor<String>();
                testQueue = TestMessageQueue.Create();
                testQueue.EnsureQueueExists();
                testQueue.Purge();
            }

            public void Dispose()
            {
                TestMessageQueue.Purge();
            }

            [MessageQueueFact]
            public void WillMoveMessagesToIntermediateProcessingQueue()
            {
                var message = testQueue.SendMessage("Test Message");

                using (new MessageReceiver<String>(TestMessageQueue.Path, new BinarySerializer(), messageProcessor))
                using (var processingQueue = TestMessageQueue.Create("processing"))
                {
                    messageProcessor.WaitForMessage();

                    Assert.NotNull(processingQueue.PeekById(message.Id));

                    messageProcessor.ProcessNextMessage();
                }
            }

            [MessageQueueFact]
            public void WillMoveMessageToPoisonQueueIfUnableToDeserializeMessage()
            {
                var message = new System.Messaging.Message("Invalid Message") { Recoverable = true };

                testQueue.Send(message);

                using (new MessageReceiver<String>(TestMessageQueue.Path, new BinarySerializer(), messageProcessor))
                {
                    testQueue.SendMessage("Valid Message");
                    messageProcessor.ProcessMessages(count: 1);
                }

                using (var poisonQueue = TestMessageQueue.Create("poison"))
                    Assert.NotNull(poisonQueue.PeekById(message.Id));
            }

            [MessageQueueFact]
            public void WillMoveMessageToPoisonQueueIfUnableToProcessMessage()
            {
                var message = testQueue.SendMessage("Test Message");

                using (new MessageReceiver<String>(TestMessageQueue.Path, new BinarySerializer(), messageProcessor))
                {
                    messageProcessor.WaitForMessage();
                    messageProcessor.ThrowException(new InvalidOperationException());
                }

                using (var poisonQueue = TestMessageQueue.Create("poison"))
                    Assert.NotNull(poisonQueue.PeekById(message.Id));
            }
        }

        public class WhenDisposing : IDisposable
        {
            public WhenDisposing()
            {
                TestMessageQueue.Create();
            }

            public void Dispose()
            {
                TestMessageQueue.Delete();
            }

            [MessageQueueFact]
            public void CanCallDisposeMoreThanOnce()
            {
                using (var receiver = new MessageReceiver<String>(TestMessageQueue.Path, new BinarySerializer(), new FakeMessageProcessor<String>()))
                {
                    receiver.Dispose();
                    receiver.Dispose();
                }
            }
        }
    }
}
