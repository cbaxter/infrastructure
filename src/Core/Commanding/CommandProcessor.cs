using System;
using System.Threading;
using System.Threading.Tasks;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Logging;
using Spark.Infrastructure.Messaging;
using Spark.Infrastructure.Resources;
using Spark.Infrastructure.Threading;

/* Copyright (c) 2012 Spark Software Ltd.
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

namespace Spark.Infrastructure.Commanding
{
    /// <summary>
    /// Executes <see cref="Command"/> instances with the associated <see cref="Aggregate"/> <see cref="CommandHandler"/>.
    /// </summary>
    public sealed class CommandProcessor : IProcessMessages<CommandEnvelope>
    {
        private static readonly TaskCreationOptions TaskCreationOptions = TaskCreationOptions.AttachedToParent | TaskCreationOptions.HideScheduler;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IRetrieveCommandHandlers commandHandlerRegistry;
        private readonly IStoreAggregates aggregateStore;
        private readonly TaskScheduler taskScheduler;
        private readonly TimeSpan retryTimeout;

        /// <summary>
        /// Creates a new instance of the <see cref="CommandProcessor"/> using the specified <see cref="IRetrieveCommandHandlers"/> and <see cref="IStoreAggregates"/> instances.
        /// </summary>
        /// <param name="commandHandlerRegistry">The <see cref="CommandHandler"/> registry.</param>
        /// <param name="aggregateStore">The <see cref="Aggregate"/> store.</param>
        public CommandProcessor(IRetrieveCommandHandlers commandHandlerRegistry, IStoreAggregates aggregateStore)
            : this(commandHandlerRegistry, aggregateStore, Settings.CommandProcessor)
        { }

        /// <summary>
        /// Creates a new instance of the <see cref="CommandProcessor"/> using the specified <see cref="IRetrieveCommandHandlers"/> and <see cref="IStoreAggregates"/> instances.
        /// </summary>
        /// <param name="commandHandlerRegistry">The <see cref="CommandHandler"/> registry.</param>
        /// <param name="aggregateStore">The <see cref="Aggregate"/> store.</param>
        /// <param name="settings">The command processor configuration settings.</param>
        internal CommandProcessor(IRetrieveCommandHandlers commandHandlerRegistry, IStoreAggregates aggregateStore, IProcessCommandSettings settings)
        {
            Verify.NotNull(commandHandlerRegistry, "commandHandlerRegistry");
            Verify.NotNull(aggregateStore, "aggregateStore");
            Verify.NotNull(settings, "settings");

            this.retryTimeout = settings.RetryTimeout;
            this.taskScheduler = new PartitionedTaskScheduler(GetAggregateId, settings.MaximumConcurrencyLevel, settings.BoundedCapacity);
            this.commandHandlerRegistry = commandHandlerRegistry;
            this.aggregateStore = aggregateStore;
        }

        /// <summary>
        /// Gets the task partition id based on the underlying command's target aggregate id.
        /// </summary>
        /// <param name="task">The task to partition.</param>
        private static Object GetAggregateId(Task task)
        {
            var message = (Message<CommandEnvelope>)task.AsyncState;

            return message.Payload.AggregateId;
        }

        /// <summary>
        /// Process the received <see cref="Command"/> message instance asynchronously.
        /// </summary>
        /// <param name="message">The <see cref="Command"/> message.</param>
        public Task ProcessAsync(Message<CommandEnvelope> message)
        {
            Verify.NotNull(message, "message");

            return Task.Factory.StartNew(state => Process((Message<CommandEnvelope>)state), message, CancellationToken.None, TaskCreationOptions, taskScheduler);
        }

        /// <summary>
        /// Process the received <see cref="Command"/> message instance synchronously.
        /// </summary>
        /// <param name="message">The <see cref="Command"/> message.</param>
        private void Process(Message<CommandEnvelope> message)
        {
            using (Log.PushContext("Message", message))
            using (var context = new CommandContext(message.Id, message.Headers))
            {
                var done = false;
                var envelope = message.Payload;
                var backoffContext = default(ExponentialBackoff);
                var commandHandler = commandHandlerRegistry.GetHandlerFor(envelope.Command);

                do
                {
                    try
                    {
                        UpdateAggregate(commandHandler, envelope, context);
                        done = true;
                    }
                    catch (ConcurrencyException ex)
                    {
                        if (backoffContext == null)
                            backoffContext = new ExponentialBackoff(retryTimeout);

                        if (!backoffContext.CanRetry)
                            throw new TimeoutException(Exceptions.UnresolvedConcurrencyConflict.FormatWith(context), ex);

                        Log.WarnFormat("Concurrency conflict: {0}", context);
                        backoffContext.WaitUntilRetry();
                    }
                } while (!done);
            }
        }

        /// <summary>
        /// Retrieves the target <see cref="Aggregate"/> instance and delegates the <see cref="Command"/> to the appropriate <see cref="CommandHandler"/>.
        /// </summary>
        /// <param name="commandHandler">The <see cref="CommandHandler"/> associated with the specified <see cref="Command"/> instance.</param>
        /// <param name="envelope">The <see cref="Command"/> envelope to process.</param>
        /// <param name="context">The underlying <see cref="CommandContext"/> associated with the specified <see cref="Command"/> instance.</param>
        private void UpdateAggregate(CommandHandler commandHandler, CommandEnvelope envelope, CommandContext context)
        {
            var aggregate = aggregateStore.Get(commandHandler.AggregateType, envelope.AggregateId);

            Log.DebugFormat("Executing {0} command handler on aggregate {1}", envelope.Command, aggregate);

            commandHandler.Handle(aggregate, envelope.Command);

            Log.Trace("Saving aggregate state");

            aggregateStore.Save(aggregate, context);

            Log.Trace("Saving state saved");
        }
    }
}
