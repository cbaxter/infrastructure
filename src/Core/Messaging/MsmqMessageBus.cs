using System;
using System.Collections.Concurrent;
using System.IO;
using System.Messaging;
using System.Threading;
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

namespace Spark.Messaging
{
    /// <summary>
    /// A MSMQ message bus for use by local and/or remote applications.
    /// </summary>
    public abstract class MsmqMessageBus
    {
        /// <summary>
        /// The <see cref="MsmqMessageBus"/> log instance.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// A MSMQ message bus for use by local and/or remote applications.
    /// </summary>
    public class MsmqMessageBus<T> : MsmqMessageBus, ISendMessages<T>, IReceiveMessages<T>, IDisposable
    {
        private readonly EventWaitHandle messageAvailable = new ManualResetEvent(initialState: false);
        private readonly EventWaitHandle idle = new ManualResetEvent(initialState: false);
        private readonly Type messageType = typeof(Message<T>);
        private readonly ISerializeObjects serializer;
        private readonly MessageQueue receiver;
        private readonly MessageQueue sender;
        private Boolean disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="MsmqMessageBus{T}"/>./// </summary>
        /// <param name="path">The MSMQ queue path.</param>
        /// <param name="serializer">The message serializer.</param>
        public MsmqMessageBus(String path, ISerializeObjects serializer)
        {
            Verify.NotNullOrWhiteSpace(path, nameof(path));
            Verify.NotNull(serializer, nameof(serializer));

            if (!MessageQueue.Exists(path)) MessageQueue.Create(path, transactional: false);
            this.receiver = new MessageQueue(path, QueueAccessMode.Receive);
            this.sender = new MessageQueue(path, QueueAccessMode.Send);
            this.receiver.PeekCompleted += SignalMessageAvailable;
            this.serializer = serializer;
            this.receiver.BeginPeek();
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="MsmqMessageBus"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            Log.TraceFormat("Disposing");

            disposed = true;
            messageAvailable.Set();
            idle.WaitOne();

            messageAvailable.Dispose();
            receiver.Dispose();
            idle.Dispose();

            Log.TraceFormat("Disposed");
        }

        /// <summary>
        /// Publishes a message on the underlying message bus.
        /// </summary>
        /// <param name="message">The message to publish on the underlying message bus.</param>
        public void Send(Message<T> message)
        {
            Verify.NotDisposed(this, disposed);
            Verify.NotNull(message, nameof(message));

            Log.TraceFormat("Sending message {0}", message.Id);

            using (var msmqMessage = new System.Messaging.Message())
            using (var memoryStream = new MemoryStream())
            {
                serializer.Serialize(memoryStream, message, messageType);
                msmqMessage.BodyStream = memoryStream;
                msmqMessage.Recoverable = true;

                lock (sender)
                {
                    sender.Send(msmqMessage);
                }
            }

            Log.TraceFormat("Message {0} sent", message.Id);
        }

        /// <summary>
        /// Blocks until a message is available or the bus has been disposed.
        /// </summary>
        /// <returns>The received message or null.</returns>
        public Message<T> Receive()
        {
            Message<T> result = null;

            if (!disposed)
            {
                Log.TraceFormat("Waiting for message");
                messageAvailable.WaitOne();
            }

            if (disposed)
            {
                idle.Set();
            }
            else
            {
                using (var message = receiver.Receive())
                    result = message == null ? null : (Message<T>)serializer.Deserialize(message.BodyStream, messageType);

                messageAvailable.Reset();
                receiver.BeginPeek();
            }

            return result;
        }

        /// <summary>
        /// Ensure a signal is set when a new message is available for processing.
        /// </summary>
        /// <param name="sender">The source of the event, the <see cref="MessageQueue"/>.</param>
        /// <param name="e">A <see cref="PeekCompletedEventArgs"/> that contains the event data.</param>
        private void SignalMessageAvailable(Object sender, PeekCompletedEventArgs e)
        {
            //TODO: Must move message to durable store while being processed to ensure fully fault tolerant.
            if (!disposed) messageAvailable.Set();
        }
    }
}
