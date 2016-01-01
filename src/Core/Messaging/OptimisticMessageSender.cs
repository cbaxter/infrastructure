using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    /// A non-durable in memory message bus based on a <see cref="BlockingCollection{T}"/> for use by single-process applications.
    /// </summary>
    public abstract class OptimisticMessageSender
    {
        /// <summary>
        /// The <see cref="OptimisticMessageSender"/> log instance.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of <see cref="OptimisticMessageSender"/>.
        /// </summary>
        internal OptimisticMessageSender()
        { }
    }

    /// <summary>
    /// A non-durable in memory message bus based on a <see cref="BlockingCollection{T}"/> for use by single-process applications.
    /// </summary>
    public class OptimisticMessageSender<T> : OptimisticMessageSender, ISendMessages<T>, IDisposable
    {
        private readonly String messageType = typeof(T).FullName;
        private readonly IDictionary<Guid, Task> tasks = new ConcurrentDictionary<Guid, Task>();
        private readonly BlockingCollection<Message<T>> messageQueue;
        private readonly IProcessMessages<T> messageProcessor;
        private readonly CancellationTokenSource tokenSource;
        private readonly Task receiverTask;
        private Boolean disposed;

        /// <summary>
        /// Gets the bounded capacity of this <see cref="OptimisticMessageSender{T}"/>.
        /// </summary>
        public Int32 BoundedCapacity { get { return messageQueue.BoundedCapacity; } }

        /// <summary>
        /// Gets the number of items queued in this <see cref="OptimisticMessageSender{T}"/>.
        /// </summary>
        public Int32 Count { get { return tasks.Count + (disposed ? 0 : messageQueue.Count); } }

        /// <summary>
        /// Initializes a new instance of <see cref="OptimisticMessageSender{T}"/>.
        /// </summary>
        /// <param name="messageProcessor">The message processor.</param>
        public OptimisticMessageSender(IProcessMessages<T> messageProcessor)
        {
            Verify.NotNull(messageProcessor, nameof(messageProcessor));

            this.messageProcessor = messageProcessor;
            this.tokenSource = new CancellationTokenSource();
            this.messageQueue = new BlockingCollection<Message<T>>();
            this.receiverTask = Task.Factory.StartNew(ReceiveAllMessages, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="OptimisticMessageSender{T}"/> with the specified <paramref name="boundedCapacity"/>.
        /// </summary>
        /// <param name="messageProcessor">The message processor.</param>
        /// <param name="boundedCapacity">The bounded size of the message bus.</param>
        public OptimisticMessageSender(IProcessMessages<T> messageProcessor, Int32 boundedCapacity)
        {
            Verify.NotNull(messageProcessor, nameof(messageProcessor));
            Verify.GreaterThan(0, boundedCapacity, nameof(boundedCapacity));

            this.messageProcessor = messageProcessor;
            this.tokenSource = new CancellationTokenSource();
            this.messageQueue = new BlockingCollection<Message<T>>(boundedCapacity);
            this.receiverTask = Task.Factory.StartNew(ReceiveAllMessages, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="OptimisticMessageSender"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            Log.Trace("Disposing");

            // Prevent new messages from being que1ued.
            disposed = true;
            messageQueue.CompleteAdding();

            // Wait for any outstanding messages to complete processing.
            WaitForDrain();
            WaitForCompletion();
            tasks.Clear();

            // Ensured all managed resources are released.
            tokenSource.Dispose();
            receiverTask.Dispose();
            messageQueue.Dispose();

            Log.Trace("Disposed");
        }

        /// <summary>
        /// Wait for all messages to be received (removed) from the underlying message queue.
        /// </summary>
        private void WaitForDrain()
        {
            Log.Trace("Waiting for message drain");

            messageQueue.CompleteAdding();
            while (!messageQueue.IsCompleted)
                Thread.Sleep(10);

            tokenSource.Cancel();
        }

        /// <summary>
        /// Wait for all outstanding messages to finish processing.
        /// </summary>
        private void WaitForCompletion()
        {
            try
            {
                Task.WaitAll(tasks.Values.ToArray());
                receiverTask.Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        /// <summary>
        /// Publishes a message on the underlying message bus.
        /// </summary>
        /// <param name="message">The message to publish on the underlying message bus.</param>
        public void Send(Message<T> message)
        {
            Verify.NotDisposed(this, disposed);
            Verify.NotNull(message, nameof(message));

            Log.Trace("Sending message {0}", message.Id);

            messageQueue.Add(message);

            Log.Trace("Message {0} sent", message.Id);
        }

        /// <summary>
        /// Receive all messages from the underlying message bus.
        /// </summary>
        private void ReceiveAllMessages()
        {
            Message<T> message;
            while ((message = Receive()) != null)
                ProcessMessage(message);
        }

        /// <summary>
        /// Blocks until a message is available or the bus has been disposed.
        /// </summary>
        /// <returns>The message received or null.</returns>
        private Message<T> Receive()
        {
            Message<T> message = null;

            try
            {
                Log.Trace("Waiting for message");

                message = messageQueue.IsCompleted ? null : messageQueue.Take(tokenSource.Token);
            }
            catch (Exception)
            {
                if (!disposed) throw;
            }

            return message;
        }

        /// <summary>
        /// Processes a message from the <see cref="messageQueue"/>.
        /// </summary>
        /// <param name="message">The MSMQ message to process.</param>
        private void ProcessMessage(Message<T> message)
        {
            Task task;

            Log.Debug("Processing {0} message {1}", messageType, message.Id);

            tasks.Add(message.Id, task = messageProcessor.ProcessAsync(message));
            task.ContinueWith(antecedent =>
            {
                if (antecedent.Status == TaskStatus.Faulted)
                {
                    using (Log.PushContext("{0} ({1})", messageType, message.Id))
                        Log.Error(antecedent.Exception);
                }

                tasks.Remove(message.Id);
            });
        }
    }
}
