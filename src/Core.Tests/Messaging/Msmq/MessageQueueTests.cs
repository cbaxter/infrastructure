using System;
using System.ComponentModel;
using System.Messaging;
using Xunit;
using MessageQueue = Spark.Messaging.Msmq.MessageQueue;

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
    namespace UsingMessageQueue
    {
        public class WhenEnsuringQueueExists
        {
            [MessageQueueFact]
            public void WillReturnTrueIfQueueCreated()
            {
                TestMessageQueue.Delete();

                using (var queue = TestMessageQueue.Create())
                    Assert.True(queue.EnsureQueueExists());
            }

            [MessageQueueFact]
            public void WillReturnTrueIfQueueAlreadyExists()
            {
                using (var queue = TestMessageQueue.Create())
                {
                    queue.EnsureQueueExists();
                    Assert.True(queue.EnsureQueueExists());
                }
            }

            [MessageQueueFact]
            public void WillReturnFalseIfUnableToCreateQueue()
            {
                using (var queue = new MessageQueue(@"~invalid~\private$\~queue~", QueueAccessMode.SendAndReceive))
                    Assert.False(queue.EnsureQueueExists());
            }
        }

        public class WhenInitializingMessageQueue
        {
            [MessageQueueFact]
            public void WillReturnTrueIfQueueCreated()
            {
                TestMessageQueue.Delete();

                Assert.True(MessageQueue.InitializeMessageQueue(TestMessageQueue.Path));
            }

            [MessageQueueFact]
            public void WillReturnTrueIfQueueAlreadyExists()
            {
                TestMessageQueue.Delete();

                MessageQueue.InitializeMessageQueue(TestMessageQueue.Path + ";subqueue");
                Assert.True(MessageQueue.InitializeMessageQueue(TestMessageQueue.Path));
            }

            [MessageQueueFact]
            public void WillReturnFalseIfUnableToCreateQueue()
            {
                Assert.False(MessageQueue.InitializeMessageQueue(@"~invalid~\private$\~queue~"));
            }
        }

        public class WhenMovingMessages : IDisposable
        {
            private readonly MessageQueue testQueue;

            public WhenMovingMessages()
            {
                testQueue = TestMessageQueue.Create();
                testQueue.EnsureQueueExists();
                testQueue.Purge();
            }

            public void Dispose()
            {
                TestMessageQueue.Purge();
            }

            [MessageQueueFact]
            public void WillMoveMessagesFromMainQueueToSubQueue()
            {
                using (var mainQueue = TestMessageQueue.Create())
                using (var subQueue = TestMessageQueue.Create("subqueue"))
                {
                    var message = new System.Messaging.Message("Message") { Recoverable = true };

                    mainQueue.Send(message);
                    mainQueue.Move(mainQueue.PeekById(message.Id), subQueue);

                    Assert.NotNull(subQueue.ReceiveById(message.Id));
                }
            }

            [MessageQueueFact]
            public void WillMoveMessagesFromSubQueueToMainQueue()
            {
                using (var mainQueue = TestMessageQueue.Create())
                using (var subQueue = TestMessageQueue.Create("subqueue"))
                {
                    var message = new System.Messaging.Message("Message") { Recoverable = true };

                    mainQueue.Send(message);
                    mainQueue.Move(mainQueue.PeekById(message.Id), subQueue);
                    subQueue.Move(subQueue.PeekById(message.Id), mainQueue);

                    Assert.NotNull(mainQueue.ReceiveById(message.Id));
                }
            }

            [MessageQueueFact]
            public void WillMoveMessagesBetweenSubQueues()
            {
                using (var mainQueue = TestMessageQueue.Create())
                using (var subQueue1 = TestMessageQueue.Create("subqueue1"))
                using (var subQueue2 = TestMessageQueue.Create("subqueue2"))
                {
                    var message = new System.Messaging.Message("Message") { Recoverable = true };

                    mainQueue.Send(message);
                    mainQueue.Move(mainQueue.PeekById(message.Id), subQueue1);
                    subQueue1.Move(subQueue1.PeekById(message.Id), subQueue2);

                    Assert.NotNull(subQueue2.ReceiveById(message.Id));
                }
            }

            [MessageQueueFact]
            public void WillNotMoveMessagesBetweenMainQueues()
            {
                var alternateQueue = new MessageQueue(TestMessageQueue.Path + ".alternate", QueueAccessMode.SendAndReceive);
                var message = new System.Messaging.Message("Message") { Recoverable = true };

                try
                {
                    alternateQueue.EnsureQueueExists();

                    testQueue.Send(message);
                    testQueue.Move(testQueue.PeekById(message.Id), alternateQueue);
                }
                catch (Win32Exception ex)
                {
                    Assert.Equal("Unknown error (0xc00e0006)", ex.Message);
                }
                finally
                {
                    System.Messaging.MessageQueue.Delete(alternateQueue.Path);
                }
            }
        }
    }
}
