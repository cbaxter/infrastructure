using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Spark.Configuration;
using Spark.Logging;
using Spark.Messaging;
using Spark.Threading;

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

namespace Spark.Cqrs.Eventing
{
    /// <summary>
    /// Executes <see cref="Event"/> instances with the associated <see cref="EventHandler"/>.
    /// </summary>
    public sealed class EventProcessor : IProcessMessages<EventEnvelope>
    {
        private static readonly TaskCreationOptions TaskCreationOptions = TaskCreationOptions.AttachedToParent | TaskCreationOptions.HideScheduler;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IDetectTransientErrors transientErrorRegistry;
        private readonly IRetrieveEventHandlers eventHandlerRegistry;
        private readonly TaskScheduler taskScheduler;
        private readonly TimeSpan retryTimeout;

        /// <summary>
        /// Creates a new instance of the <see cref="EventProcessor"/> using the specified <see cref="IRetrieveEventHandlers"/> instance.
        /// </summary>
        /// <param name="eventHandlerRegistry">The <see cref="EventHandler"/> registry.</param>
        /// <param name="transientErrorRegistries">The set of <see cref="IDetectTransientErrors"/> instances used to detect transient errors.</param>
        public EventProcessor(IRetrieveEventHandlers eventHandlerRegistry, IEnumerable<IDetectTransientErrors> transientErrorRegistries)
            : this(eventHandlerRegistry, new TransientErrorRegistry(transientErrorRegistries), Settings.EventProcessor)
        { }

        /// <summary>
        /// Creates a new instance of the <see cref="EventProcessor"/> using the specified <see cref="IRetrieveEventHandlers"/> instance.
        /// </summary>
        /// <param name="eventHandlerRegistry">The <see cref="EventHandler"/> registry.</param>
        /// <param name="transientErrorRegistry">The <see cref="IDetectTransientErrors"/> instance used to detect transient errors.</param>
        /// <param name="settings">The event processor configuration settings.</param>
        internal EventProcessor(IRetrieveEventHandlers eventHandlerRegistry, IDetectTransientErrors transientErrorRegistry, IProcessEventSettings settings)
        {
            Verify.NotNull(eventHandlerRegistry, nameof(eventHandlerRegistry));
            Verify.NotNull(settings, nameof(settings));

            this.retryTimeout = settings.RetryTimeout;
            this.eventHandlerRegistry = eventHandlerRegistry;
            this.transientErrorRegistry = transientErrorRegistry;
            this.taskScheduler = new PartitionedTaskScheduler(GetAggregateId, settings.MaximumConcurrencyLevel, settings.BoundedCapacity);
        }

        /// <summary>
        /// Gets the task partition id based on the underlying event's source aggregate id.
        /// </summary>
        /// <param name="task">The task to partition.</param>
        private static Object GetAggregateId(Task task)
        {
            var message = (Message<EventEnvelope>)task.AsyncState;

            return message.Payload.AggregateId;
        }

        /// <summary>
        /// Process the received <see cref="Event"/> message instance asynchronously.
        /// </summary>
        /// <param name="message">The <see cref="Event"/> message.</param>
        public Task ProcessAsync(Message<EventEnvelope> message)
        {
            Verify.NotNull(message, nameof(message));

            return CreateTask(message);
        }

        /// <summary>
        /// Create a new worker task that will be used to process the specified <see cref="Event"/> message.
        /// </summary>
        /// <param name="message">The <see cref="Event"/> message.</param>
        private Task CreateTask(Message<EventEnvelope> message)
        {
            var task = Task.Factory.StartNew(state => Process((Message<EventEnvelope>)state), message, CancellationToken.None, TaskCreationOptions, taskScheduler);

            task.ConfigureAwait(continueOnCapturedContext: false);

            return task;
        }

        /// <summary>
        /// Process the received <see cref="Event"/> message instance synchronously.
        /// </summary>
        /// <param name="message">The <see cref="Event"/> message.</param>
        public void Process(Message<EventEnvelope> message)
        {
            var envelope = message.Payload;

            using (Log.PushContext("{0} ({1})", message.Payload.Event.GetType(), message.Id))
            using (var context = new EventContext(envelope.AggregateId, message.Headers, envelope.Event))
            {
                var eventHandlers = eventHandlerRegistry.GetHandlersFor(envelope.Event);

                foreach (var eventHandler in eventHandlers)
                    ExecuteHandler(eventHandler, context);
            }
        }

        /// <summary>
        /// Process the received <see cref="Event"/> using the specified <paramref name="eventHandler"/>.
        /// </summary>
        /// <param name="eventHandler">The <see cref="EventHandler"/> to be executed.</param>
        /// <param name="context">The underlying <see cref="EventContext"/> to use when executing the specified <paramref name="eventHandler"/>.</param>
        private void ExecuteHandler(EventHandler eventHandler, EventContext context)
        {
            var backoffContext = default(ExponentialBackoff);
            var done = false;

            do
            {
                try
                {
                    eventHandler.Handle(context);

                    done = true;
                }
                catch (Exception ex)
                {
                    if (!transientErrorRegistry.IsTransient(ex))
                        throw;

                    backoffContext = backoffContext ?? new ExponentialBackoff(retryTimeout);
                    backoffContext.WaitOrTimeout(ex);
                    Log.Warn(ex.Message);
                }
            } while (!done);
        }
    }
}
