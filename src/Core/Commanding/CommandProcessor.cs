using System;
using System.Threading;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Logging;
using Spark.Infrastructure.Messaging;

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
        /// <param name="retryTimeout">The maximum amount of time to retry processing a given command.</param>
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
        /// <param name="commandId">The unique <paramref name="command"/> instance id.</param>
        /// <param name="headers">The message headers associated with the <paramref name="command"/>.</param>
        /// <param name="command">The <see cref="Command"/> to process.</param>
        public void Process(Guid commandId, HeaderCollection headers, Command command)
        {
            var endTime = default(DateTime?);
            var timeout = TimeSpan.Zero;
            var processed = false;

            do
            {
                try
                {
                    UpdateAggregate(commandId, headers, command);
                    processed = true;
                }
                catch (ConcurrencyException)
                {
                    Log.WarnFormat("Concurrency conflict: {0}", command);
                }
                catch (Exception ex)
                {
                    if (CanRetry(ref endTime, ref timeout))
                    {
                        Log.Warn(ex.Message);
                        Log.TraceFormat("Waiting {0}", timeout);

                        Thread.Sleep(timeout);
                    }
                    else
                    {
                        Log.Error(ex);
                        processed = true;
                    }
                }
            } while (!processed);
        }

        /// <summary>
        /// Retrieves the target <see cref="Aggregate"/> instance and delegates the <paramref name="command"/> to the appropriate <see cref="CommandHandler"/>.
        /// </summary>
        /// <param name="commandId">The unique <paramref name="command"/> instance id.</param>
        /// <param name="headers">The message headers associated with the <paramref name="command"/>.</param>
        /// <param name="command">The <see cref="Command"/> to process.</param>
        private void UpdateAggregate(Guid commandId, HeaderCollection headers, Command command)
        {
            var commandHandler = commandHandlerRegistry.GetHandlerFor(command);

            using (var context = new CommandContext(commandId, headers))
            {
                var aggregate = aggregateStore.Get(commandHandler.AggregateType, command.AggregateId);

                Log.Trace("Executing command handler");

                commandHandler.Handle(aggregate, command);

                Log.Trace("Saving aggregate state");

                aggregateStore.Save(aggregate, context);

                Log.Trace("Saving state saved");
            }
        }

        /// <summary>
        /// Determine if another attempt should be made to process the command. 
        /// </summary>
        /// <param name="endTime">The absolute end time to stop processing this command.</param>
        /// <param name="timeout">The timeout between processing attempts.</param>
        private Boolean CanRetry(ref DateTime? endTime, ref TimeSpan timeout)
        {
            var now = SystemTime.Now;
            var canRetry = false;

            if (!endTime.HasValue)
                endTime = now.Add(retryTimeout);

            if (now < endTime)
            {
                if (timeout == TimeSpan.Zero)
                {
                    timeout = TimeSpan.FromMilliseconds(5);
                }
                else
                {
                    timeout = TimeSpan.FromMilliseconds(timeout.TotalMilliseconds * 2);

                    var timeRemaining = endTime.Value.Subtract(now);
                    if (timeout > timeRemaining)
                        timeout = timeRemaining;
                }

                canRetry = true;
            }

            return canRetry;
        }
    }
}
