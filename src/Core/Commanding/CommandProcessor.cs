using System;
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
    public sealed class CommandProcessor : IProcessCommands
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IRetrieveCommandHandlers commandHandlerRegistry;
        private readonly IStoreAggregates aggregateStore;
        private readonly TimeSpan retryTimeout;

        /// <summary>
        /// Creates a new instance of the <see cref="CommandProcessor"/> using the specified <see cref="IRetrieveCommandHandlers"/> and <see cref="IStoreAggregates"/> instances.
        /// </summary>
        /// <param name="commandHandlerRegistry">The <see cref="CommandHandler"/> registry.</param>
        /// <param name="aggregateStore">The <see cref="Aggregate"/> store.</param>
        public CommandProcessor(IRetrieveCommandHandlers commandHandlerRegistry, IStoreAggregates aggregateStore)
            : this(commandHandlerRegistry, aggregateStore, Settings.CommandProcessor.RetryTimeout)
        { }

        /// <summary>
        /// Creates a new instance of the <see cref="CommandProcessor"/> using the specified <see cref="IRetrieveCommandHandlers"/> and <see cref="IStoreAggregates"/> instances.
        /// </summary>
        /// <param name="commandHandlerRegistry">The <see cref="CommandHandler"/> registry.</param>
        /// <param name="aggregateStore">The <see cref="Aggregate"/> store.</param>
        /// <param name="retryTimeout">The maximum amount of time to try processing a given command.</param>
        internal CommandProcessor(IRetrieveCommandHandlers commandHandlerRegistry, IStoreAggregates aggregateStore, TimeSpan retryTimeout)
        {
            Verify.NotNull(commandHandlerRegistry, "commandHandlerRegistry");
            Verify.NotNull(aggregateStore, "aggregateStore");

            this.commandHandlerRegistry = commandHandlerRegistry;
            this.aggregateStore = aggregateStore;
            this.retryTimeout = retryTimeout;
        }

        /// <summary>
        /// Processes a given <see cref="Command"/> instance.
        /// </summary>
        /// <param name="commandId">The unique <see cref="Command"/> instance id.</param>
        /// <param name="headers">The message headers associated with the <paramref name="envelope"/>.</param>
        /// <param name="envelope">The <see cref="Command"/> envelope to process.</param>
        public void Process(Guid commandId, HeaderCollection headers, CommandEnvelope envelope)
        {
            Verify.NotEqual(Guid.Empty, commandId, "commandId");
            Verify.NotNull(envelope, "envelope");
            Verify.NotNull(headers, "headers");

            using (var context = new CommandContext(commandId, headers, envelope))
            {
                var commandHandler = commandHandlerRegistry.GetHandlerFor(context.Command);
                var backoffContext = default(ExponentialBackoff);
                var done = false;

                do
                {
                    try
                    {
                        UpdateAggregate(context, commandHandler);
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
        /// <param name="context">The underlying <see cref="CommandContext"/> associated with the specified <see cref="Command"/> instance.</param>
        /// <param name="commandHandler">The <see cref="CommandHandler"/> associated with the specified <see cref="Command"/> instance.</param>
        private void UpdateAggregate(CommandContext context, CommandHandler commandHandler)
        {
            var aggregate = aggregateStore.Get(commandHandler.AggregateType, context.AggregateId);

            Log.Trace("Executing command handler");

            commandHandler.Handle(aggregate, context.Command);

            Log.Trace("Saving aggregate state");

            aggregateStore.Save(aggregate, context);

            Log.Trace("Saving state saved");
        }
    }
}
