using System;
using System.Collections.Concurrent;
using System.Threading;
using Spark.Logging;

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
    /// An in memory message bus based on a <see cref="BlockingCollection{T}"/> for use by single-process applications.
    /// </summary>
    public abstract class BlockingCollectionMessageBus
    {
        /// <summary>
        /// The <see cref="BlockingCollectionMessageBus"/> log instance.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of <see cref="BlockingCollectionMessageBus"/>.
        /// </summary>
        internal BlockingCollectionMessageBus()
        { }
    }

    /// <summary>
    /// An in memory message bus based on a <see cref="BlockingCollection{T}"/> for use by single-process applications.
    /// </summary>
    public class BlockingCollectionMessageBus<T> : BlockingCollectionMessageBus, ISendMessages<T>, IReceiveMessages<T>, IDisposable
    {
        private readonly BlockingCollection<Message<T>> messageQueue;
        private readonly CancellationTokenSource tokenSource;
        private Boolean disposed;

        /// <summary>
        /// Gets the bounded capacity of this <see cref="BlockingCollectionMessageBus{T}"/>.
        /// </summary>
        public Int32 BoundedCapacity { get { return messageQueue.BoundedCapacity; } }

        /// <summary>
        /// Gets the number of items queued in this <see cref="BlockingCollectionMessageBus{T}"/>.
        /// </summary>
        public Int32 Count { get { return disposed ? 0 : messageQueue.Count; } }

        /// <summary>
        /// Initializes a new instance of <see cref="BlockingCollectionMessageBus{T}"/>.
        /// </summary>
        public BlockingCollectionMessageBus()
        {
            messageQueue = new BlockingCollection<Message<T>>();
            tokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BlockingCollectionMessageBus{T}"/> with the specified <paramref name="boundedCapacity"/>.
        /// </summary>
        /// <param name="boundedCapacity">The bounded size of the message bus.</param>
        public BlockingCollectionMessageBus(Int32 boundedCapacity)
        {
            Verify.GreaterThan(0, boundedCapacity, "boundedCapacity");

            messageQueue = new BlockingCollection<Message<T>>(boundedCapacity);
            tokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Wait for all messages to be received (removed) from the underlying message queue.
        /// </summary>
        public void WaitForDrain()
        {
            Verify.NotDisposed(this, disposed);

            Log.TraceFormat("Waiting for message drain");
            messageQueue.CompleteAdding();
            while (!messageQueue.IsCompleted)
                Thread.Sleep(10);

            Log.TraceFormat("Cancel all blocked receive operations");
            tokenSource.Cancel();
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="BlockingCollectionMessageBus"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            Log.TraceFormat("Disposing");

            WaitForDrain();
            tokenSource.Dispose();
            messageQueue.Dispose();
            disposed = true;

            Log.TraceFormat("Disposed");
        }

        /// <summary>
        /// Publishes a message on the underlying message bus.
        /// </summary>
        /// <param name="message">The message to publish on the underlying message bus.</param>
        public void Send(Message<T> message)
        {
            Verify.NotDisposed(this, disposed);
            Verify.NotNull(message, "message");

            Log.TraceFormat("Sending message {0}", message.Id);

            messageQueue.Add(message);

            Log.TraceFormat("Message {0} sent", message.Id);
        }

        /// <summary>
        /// Blocks until a message is available or the bus has been disposed.
        /// </summary>
        /// <returns>The message received or null.</returns>
        public Message<T> Receive()
        {
            Message<T> message = null;

            if (!(disposed || messageQueue.IsCompleted))
            {
                try
                {
                    Log.TraceFormat("Waiting for message");
                    message = messageQueue.Take(tokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    Log.Warn("Operation cancelled");
                }
            }

            return message;
        }
    }
}
