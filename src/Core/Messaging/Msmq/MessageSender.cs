using System;
using System.IO;
using System.Messaging;
using Spark.Logging;
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

namespace Spark.Messaging.Msmq
{
    /// <summary>
    /// Sends messages to the underlying MSMQ message queue.
    /// </summary>
    public abstract class MessageSender
    {
        /// <summary>
        /// The <see cref="MessageSender"/> log instance.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Sends messages to the underlying MSMQ message queue.
    /// </summary>
    public sealed class MessageSender<T> : MessageSender, ISendMessages<T>
    {
        private readonly Type messageType = typeof(Message<T>);
        private readonly ISerializeObjects serializer;
        private readonly String path;

        /// <summary>
        /// Initializes a new instance of <see cref="MessageSender{T}"/></summary>
        /// <param name="path">The MSMQ queue path.</param>
        /// <param name="serializer">The message serializer.</param>
        public MessageSender(String path, ISerializeObjects serializer)
        {
            Verify.NotNullOrWhiteSpace(path, nameof(path));
            Verify.NotNull(serializer, nameof(serializer));

            MessageQueue.InitializeMessageQueue(path);

            this.serializer = serializer;
            this.path = path;
        }

        /// <summary>
        /// Publishes a message on the underlying message bus.
        /// </summary>
        /// <param name="message">The message to publish on the underlying message bus.</param>
        public void Send(Message<T> message)
        {
            Verify.NotNull(message, nameof(message));

            Log.Trace("Sending message {0}", message.Id);

            using (var messageQueue = new System.Messaging.MessageQueue(path, QueueAccessMode.Send))
            using (var msmqMessage = new System.Messaging.Message())
            using (var memoryStream = new MemoryStream())
            {
                serializer.Serialize(memoryStream, message, messageType);
                msmqMessage.BodyStream = memoryStream;
                msmqMessage.Recoverable = true;

                messageQueue.Send(msmqMessage);

                Log.Debug("MSMQ message {0} sent", msmqMessage.Id);
            }

            Log.Trace("Message {0} sent", message.Id);
        }
    }
}
