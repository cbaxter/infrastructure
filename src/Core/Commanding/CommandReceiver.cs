using System;
using System.Threading;
using System.Threading.Tasks;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Logging;
using Spark.Infrastructure.Messaging;
using Spark.Infrastructure.Threading;
using CommandMessage = Spark.Infrastructure.Messaging.Message<Spark.Infrastructure.Commanding.CommandEnvelope>;

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
    /// Receives commands from the underlying <see cref="Command"/> message bus and delegates to a <see cref="IProcessCommands"/> instance.
    /// </summary>
    public sealed class CommandReceiver : IDisposable
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IReceiveMessages<CommandEnvelope> messageReceiver;
        private readonly IProcessCommands commandProcessor;
        private readonly TaskScheduler taskScheduler;
        private readonly Task receiverTask;
        private Boolean disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="CommandReceiver"/> using the specified <see cref="IReceiveMessages{Command}"/> and <see cref="IProcessCommands"/> instances.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="commandProcessor">The command processor.</param>
        public CommandReceiver(IReceiveMessages<CommandEnvelope> messageReceiver, IProcessCommands commandProcessor)
            : this(messageReceiver, commandProcessor, new PartitionedTaskScheduler(GetPartitionId, Settings.CommandReceiver.MaximumConcurrencyLevel, Settings.CommandReceiver.BoundedCapacity))
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="CommandReceiver"/> using the specified <see cref="IReceiveMessages{Command}"/>, <see cref="IProcessCommands"/> and <see cref="TaskScheduler"/> instances.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="commandProcessor">The command processor.</param>
        /// <param name="taskScheduler">The task scheduler.</param>
        internal CommandReceiver(IReceiveMessages<CommandEnvelope> messageReceiver, IProcessCommands commandProcessor, TaskScheduler taskScheduler)
        {
            Verify.NotNull(taskScheduler, "taskScheduler");
            Verify.NotNull(messageReceiver, "messageReceiver");
            Verify.NotNull(commandProcessor, "commandProcessor");

            this.taskScheduler = taskScheduler;
            this.messageReceiver = messageReceiver;
            this.commandProcessor = commandProcessor;
            this.receiverTask = Task.Factory.StartNew(ReceiveAllMessages, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="CommandReceiver"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            receiverTask.Wait();
            receiverTask.Dispose();
        }

        /// <summary>
        /// Gets the task partition id based on the underlying command's target aggregate id.
        /// </summary>
        /// <param name="task">The task to partition.</param>
        private static Object GetPartitionId(Task task)
        {
            var message = (CommandMessage)task.AsyncState;

            return message.Payload == null ? Guid.Empty : message.Payload.AggregateId;
        }

        /// <summary>
        /// Receive all messages from the underlying <see cref="Command"/> message bus.
        /// </summary>
        private void ReceiveAllMessages()
        {
            CommandMessage message;
            while ((message = messageReceiver.Receive()) != null)
            {
                Log.TraceFormat("Message received: {0}", message);

                Task.Factory.StartNew(m => ProcessMessage((CommandMessage)m), message, CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler);
            }
        }

        /// <summary>
        /// Process the received <see cref="Command"/> message instance.
        /// </summary>
        /// <param name="message">The <see cref="Command"/> message.</param>
        private void ProcessMessage(CommandMessage message)
        {
            using (Log.PushContext("Message", message))
            {
                try
                {
                    var payload = message.Payload;
                    if (payload == null)
                    {
                        Log.WarnFormat("Message payload empty; no action required");
                    }
                    else
                    {
                        Log.Trace("Processing command");

                        commandProcessor.Process(message.Id, message.Headers, payload);

                        Log.Trace("Command processed");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }
    }
}
