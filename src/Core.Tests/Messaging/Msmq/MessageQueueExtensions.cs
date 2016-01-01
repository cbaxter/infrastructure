using System;
using System.IO;
using Spark.Messaging;
using Spark.Messaging.Msmq;
using Spark.Serialization;

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
    /// <summary>
    /// Extension methods of <see cref="MessageQueue"/> used to support unit tests.
    /// </summary>
    internal static class MessageQueueExtensions
    {
        /// <summary>
        /// Sends a message to the specified message queue.
        /// </summary>
        /// <param name="queue">The message queue.</param>
        /// <param name="payload">The message payload to send.</param>
        public static System.Messaging.Message SendMessage<T>(this MessageQueue queue, T payload)
        {
            var message = new Message<T>(Guid.NewGuid(), HeaderCollection.Empty, payload);
            var serializer = new BinarySerializer();

            using (var msmqMessage = new System.Messaging.Message())
            using (var memoryStream = new MemoryStream())
            {
                serializer.Serialize(memoryStream, message, message.GetType());
                msmqMessage.BodyStream = memoryStream;
                msmqMessage.Recoverable = true;

                queue.Send(msmqMessage);

                return queue.PeekById(msmqMessage.Id);
            }
        }
    }
}
