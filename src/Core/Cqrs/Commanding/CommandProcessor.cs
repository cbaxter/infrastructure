using System;
using System.Threading;
using System.Threading.Tasks;
using Spark.Configuration;
using Spark.Cqrs.Domain;
using Spark.EventStore;
using Spark.Logging;
using Spark.Messaging;
using Spark.Resources;
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

namespace Spark.Cqrs.Commanding
{
    /// <summary>
    /// Executes <see cref="Command"/> instances with the associated <see cref="Aggregate"/> <see cref="CommandHandler"/>.
    /// </summary>
    public sealed class CommandProcessor : IProcessMessages<CommandEnvelope>
    {
        private static readonly TaskCreationOptions TaskCreationOptions = TaskCreationOptions.AttachedToParent | TaskCreationOptions.HideScheduler;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IRetrieveCommandHandlers commandHandlerRegistry;
        private readonly TaskScheduler taskScheduler;
        private readonly TimeSpan retryTimeout;

        /// <summary>
        /// Creates a new instance of the <see cref="CommandProcessor"/> using the specified <see cref="IRetrieveCommandHandlers"/> and <see cref="IStoreAggregates"/> instances.
        /// </summary>
        /// <param name="commandHandlerRegistry">The <see cref="CommandHandler"/> registry.</param>
        public CommandProcessor(IRetrieveCommandHandlers commandHandlerRegistry)
            : this(commandHandlerRegistry, Settings.CommandProcessor)
        { }

        /// <summary>
        /// Creates a new instance of the <see cref="CommandProcessor"/> using the specified <see cref="IRetrieveCommandHandlers"/> and <see cref="IStoreAggregates"/> instances.
        /// </summary>
        /// <param name="commandHandlerRegistry">The <see cref="CommandHandler"/> registry.</param>
        /// <param name="settings">The command processor configuration settings.</param>
        internal CommandProcessor(IRetrieveCommandHandlers commandHandlerRegistry, IProcessCommandSettings settings)
        {
            Verify.NotNull(commandHandlerRegistry, "commandHandlerRegistry");
            Verify.NotNull(settings, "settings");

            this.retryTimeout = settings.RetryTimeout;
            this.taskScheduler = new PartitionedTaskScheduler(GetAggregateId, settings.MaximumConcurrencyLevel, settings.BoundedCapacity);
            this.commandHandlerRegistry = commandHandlerRegistry;
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
            {
                var commandHandler = commandHandlerRegistry.GetHandlerFor(message.Payload.Command);

                ExecuteHandler(commandHandler, message);
            }
        }

        /// <summary>
        ///  Process the received <see cref="Command"/> message instance using the specified <paramref name="commandHandler"/>.
        /// </summary>
        /// <param name="commandHandler">The <see cref="CommandHandler"/> instance that will process the <paramref name="message"/>.</param>
        /// <param name="message">The <see cref="Command"/> message.</param>
        private void ExecuteHandler(CommandHandler commandHandler, Message<CommandEnvelope> message)
        {
            var backoffContext = default(ExponentialBackoff);
            var done = false;

            do
            {
                try
                {
                    using (var context = new CommandContext(message.Id, message.Headers, message.Payload))
                        commandHandler.Handle(context);

                    done = true;
                }
                catch (ConcurrencyException ex)
                {
                    if (backoffContext == null)
                        backoffContext = new ExponentialBackoff(retryTimeout);

                    if (!backoffContext.CanRetry)
                        throw new TimeoutException(Exceptions.UnresolvedConcurrencyConflict.FormatWith(message.Payload), ex);

                    Log.WarnFormat("Concurrency conflict: {0}", message.Payload);
                    backoffContext.WaitUntilRetry();
                }
            } while (!done);
        }
    }
}
