using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Threading.Tasks;
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
    /// Receives messages from the underlying MSMQ message queue.
    /// </summary>
    public abstract class MessageReceiver
    {
        /// <summary>
        /// A <see cref="MessagePropertyFilter"/> instance used to filter peeked/received message properties to the LookupId reference only.
        /// </summary>
        protected static readonly MessagePropertyFilter ReferenceOnly = new MessagePropertyFilter { Id = true, LookupId = true };

        /// <summary>
        /// A <see cref="MessagePropertyFilter"/> instance used to filter peeked/received message properties to the LookupId reference and associated Body.
        /// </summary>
        protected static readonly MessagePropertyFilter MessageBodyOnly = new MessagePropertyFilter { Id = true, LookupId = true, Body = true };

        /// <summary>
        /// The <see cref="MessageReceiver"/> log instance.
        /// </summary>
        protected static readonly ILog Log = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Receives messages from the underlying MSMQ message queue.
    /// </summary>
    public class MessageReceiver<T> : MessageReceiver, IDisposable
    {
        private readonly IDictionary<Int64, Task> tasks = new ConcurrentDictionary<Int64, Task>();
        private readonly Object criticalRegion = new Object();
        private readonly Type messageType = typeof(Message<T>);
        private readonly IProcessMessages<T> messageProcessor;
        private readonly ISerializeObjects serializer;
        private readonly MessageQueue processingQueue;
        private readonly MessageQueue pendingQueue;
        private readonly MessageQueue poisonQueue;
        private Task receiverTask;
        private Boolean disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="MessageReceiver{T}"/></summary>
        /// <param name="path">The MSMQ queue path.</param>
        /// <param name="serializer">The message serializer.</param>
        /// <param name="messageProcessor">The message processor.</param>
        public MessageReceiver(String path, ISerializeObjects serializer, IProcessMessages<T> messageProcessor)
        {
            Verify.NotNullOrWhiteSpace(path, nameof(path));
            Verify.NotNull(serializer, nameof(serializer));
            Verify.NotNull(messageProcessor, nameof(messageProcessor));

            MessageQueue.InitializeMessageQueue(path);

            this.serializer = serializer;
            this.messageProcessor = messageProcessor;
            this.pendingQueue = new MessageQueue(path, QueueAccessMode.Receive) { MessageReadPropertyFilter = MessageBodyOnly };
            this.poisonQueue = new MessageQueue(path + ";poison", QueueAccessMode.Receive) { MessageReadPropertyFilter = MessageBodyOnly };
            this.processingQueue = new MessageQueue(path + ";processing", QueueAccessMode.Receive) { MessageReadPropertyFilter = MessageBodyOnly };
            this.receiverTask = Task.Run(() => EnureProcessingQueueEmpty());
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="MessageReceiver{T}"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            Log.Trace("Disposing");

            // Ensure we are not in the process of scheduling a new receiver task
            lock (criticalRegion)
            {
                disposed = true;
            }

            // Wait for in-flight operations to complete.
            Task.WaitAll(receiverTask);
            Task.WaitAll(tasks.Values.ToArray());

            // Dispose underlying resources.
            pendingQueue.Dispose();
            processingQueue.Dispose();
            poisonQueue.Dispose();
            tasks.Clear();

            Log.Trace("Disposed");
        }

        /// <summary>
        /// Ensures any messages in the <see cref="processingQueue"/> subqueue are processed before handling any new messages in the <see cref="pendingQueue"/>.
        /// </summary>
        private void EnureProcessingQueueEmpty()
        {
            ProcessMessages(processingQueue, ProcessMessage);

            if (!disposed)
            {
                pendingQueue.PeekCompleted += (sender, e) =>
                {
                    lock (criticalRegion)
                    {
                        if (!disposed) receiverTask = Task.Run(() => ProcessPendingMessages());
                    }
                };
                pendingQueue.BeginPeek();
            }
        }

        /// <summary>
        /// Process any pending messages in the underlying message queue.
        /// </summary>
        private void ProcessPendingMessages()
        {
            ProcessMessages(pendingQueue, ProcessPendingMessage);

            if (!disposed)
            {
                pendingQueue.BeginPeek();
            }
        }

        /// <summary>
        /// Processes all current messages in the underlying message queue.
        /// </summary>
        /// <param name="messageQueue">The MSMQ message queue instance for which any messages are to be processed.</param>
        /// <param name="handler">The message handler to invoke when a message is found.</param>
        private void ProcessMessages(System.Messaging.MessageQueue messageQueue, Action<System.Messaging.Message> handler)
        {
            using (var messages = messageQueue.GetMessageEnumerator2())
            {
                while (messages.MoveNext())
                {
                    var message = messages.Current;

                    if (disposed) break;
                    if (message == null) continue;

                    handler.Invoke(message);
                }
            }
        }

        /// <summary>
        /// Processes a message from the <see cref="pendingQueue"/>.
        /// </summary>
        /// <param name="message">The MSMQ message to process.</param>
        private void ProcessPendingMessage(System.Messaging.Message message)
        {
            pendingQueue.Move(message, processingQueue);

            ProcessMessage(message);
        }

        /// <summary>
        /// Processes a message from the <see cref="processingQueue"/>.
        /// </summary>
        /// <param name="message">The MSMQ message to process.</param>
        private void ProcessMessage(System.Messaging.Message message)
        {
            Log.Debug("Processing {0} message {1}", pendingQueue.Path, message.Id);

            try
            {
                var messageBody = (Message<T>)serializer.Deserialize(message.BodyStream, messageType);
                var task = messageProcessor.ProcessAsync(messageBody).ContinueWith(MoveOrRemoveMessageFromQueue, message);

                tasks.Add(message.LookupId, task);
                task.ContinueWith(RemoveFromTasks, message);
            }
            catch (Exception ex)
            {
                MoveMessageToPoisonQueue(message, ex);
            }
        }

        /// <summary>
        /// Removes the message from the queue if processed successfully; otherwise moves the message to the poison queue if an exception was thrown.
        /// </summary>
        /// <param name="task">The underlying worker task that processed the <paramref name="state"/> message.</param>
        /// <param name="state">The <see cref="System.Messaging.Message"/> that was processed.</param>
        private void MoveOrRemoveMessageFromQueue(Task task, Object state)
        {
            var message = (System.Messaging.Message)state;
            var lookupId = message.LookupId;

            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    RemoveMessageFromQueue(lookupId);
                    break;
                case TaskStatus.Faulted:
                    MoveMessageToPoisonQueue(message, task.Exception);
                    break;
            }
        }

        /// <summary>
        /// Removes the task reference from <see cref="tasks"/> once the message has been processed.
        /// </summary>
        /// <remarks>
        /// Intentionally kept as a separate continuation to ensure that message clean-up has completed before
        /// removing from the active <see cref="tasks"/> collection. A separate continuation is required as it 
        /// is possible for a task to complete synchronously and thus never get removed from <see cref="tasks"/>.
        /// </remarks>
        /// <param name="task">The underlying worker task that processed the <paramref name="state"/> message.</param>
        /// <param name="state">The <see cref="System.Messaging.Message"/> that was processed.</param>
        private void RemoveFromTasks(Task task, Object state)
        {
            var message = (System.Messaging.Message)state;

            tasks.Remove(message.LookupId);
        }

        /// <summary>
        /// Removes the message identified by the specified <paramref name="lookupId"/> from the underlying message queue.
        /// </summary>
        /// <param name="lookupId">The unique message lookup identifier.</param>
        private void RemoveMessageFromQueue(Int64 lookupId)
        {
            using (var messageQueue = new System.Messaging.MessageQueue(processingQueue.Path, QueueAccessMode.Receive) { MessageReadPropertyFilter = ReferenceOnly })
                messageQueue.ReceiveByLookupId(lookupId);
        }

        /// <summary>
        /// Moves the specified <paramref name="message"/> to the poison message queue and logs the exception that caused the message to be rejected.
        /// </summary>
        /// <param name="message">The <see cref="System.Messaging.Message"/> that cannot be processed.</param>
        /// <param name="ex">The exception that caused the message processing to terminate.</param>
        private void MoveMessageToPoisonQueue(System.Messaging.Message message, Exception ex)
        {
            using (Log.PushContext("{0} ({1})", pendingQueue.Path, message.Id))
                Log.Error(ex);

            lock (poisonQueue)
            {
                processingQueue.Move(message, poisonQueue);
            }
        }
    }
}
