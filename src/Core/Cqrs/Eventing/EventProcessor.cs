using System;
using System.Threading;
using System.Threading.Tasks;
using Spark.Configuration;
using Spark.Logging;
using Spark.Messaging;
using Spark.Threading;

/* Copyright (c) 2013 Spark Software Ltd.
 * 
 * This source is subject to the GNU Lesser General Public License.
 * See: http://www.gnu.org/copyleft/lesser.html
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE. 
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
        private readonly IRetrieveEventHandlers eventHandlerRegistry;
        private readonly TaskScheduler taskScheduler;
        private readonly TimeSpan retryTimeout;

        /// <summary>
        /// Creates a new instance of the <see cref="EventProcessor"/> using the specified <see cref="IRetrieveEventHandlers"/> instance.
        /// </summary>
        /// <param name="eventHandlerRegistry">The <see cref="EventHandler"/> registry.</param>
        public EventProcessor(IRetrieveEventHandlers eventHandlerRegistry)
            : this(eventHandlerRegistry, Settings.EventProcessor)
        { }

        /// <summary>
        /// Creates a new instance of the <see cref="EventProcessor"/> using the specified <see cref="IRetrieveEventHandlers"/> instance.
        /// </summary>
        /// <param name="eventHandlerRegistry">The <see cref="EventHandler"/> registry.</param>
        /// <param name="settings">The event processor configuration settings.</param>
        internal EventProcessor(IRetrieveEventHandlers eventHandlerRegistry, IProcessEventSettings settings)
        {
            Verify.NotNull(eventHandlerRegistry, "eventHandlerRegistry");
            Verify.NotNull(settings, "settings");

            this.retryTimeout = settings.RetryTimeout;
            this.eventHandlerRegistry = eventHandlerRegistry;
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
            Verify.NotNull(message, "message");

            return Task.Factory.StartNew(state => Process((Message<EventEnvelope>)state), message, CancellationToken.None, TaskCreationOptions, taskScheduler);
        }

        /// <summary>
        /// Process the received <see cref="Event"/> message instance synchronously.
        /// </summary>
        /// <param name="message">The <see cref="Event"/> message.</param>
        private void Process(Message<EventEnvelope> message)
        {
            var envelope = message.Payload;

            using (Log.PushContext("Message", message))
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
                    if (backoffContext == null)
                        backoffContext = new ExponentialBackoff(retryTimeout);

                    if (!backoffContext.CanRetry)
                        throw new TimeoutException(ex.Message, ex);

                    Log.WarnFormat(ex.Message);
                    backoffContext.WaitUntilRetry();
                }
            } while (!done);
        }
    }
}
