using System;
using System.Threading.Tasks;
using Spark.Logging;

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

namespace Spark.Messaging
{
    /// <summary>
    /// Receives messages from the underlying <see cref="IReceiveMessages{T}"/> message bus and delegates to a <see cref="IProcessMessages{T}"/> processor instance.
    /// </summary>
    public abstract class MessageReceiver
    {
        protected static readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of <see cref="MessageReceiver"/>.
        /// </summary>
        internal MessageReceiver()
        { }
    }

    /// <summary>
    /// Receives messages from the underlying <see cref="IReceiveMessages{T}"/> message bus and delegates to a <see cref="IProcessMessages{T}"/> processor instance.
    /// </summary>
    public sealed class MessageReceiver<T> : MessageReceiver, IDisposable
    {
        private readonly IProcessMessages<T> messageProcessor;
        private readonly IReceiveMessages<T> messageReceiver;
        private readonly Task receiverTask;
        private Boolean disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="MessageReceiver"/> using the specified <see cref="IReceiveMessages{T}"/> and <see cref="IProcessMessages{T}"/> instances.
        /// </summary>
        /// <param name="messageReceiver">The message receiver.</param>
        /// <param name="messageProcessor">The message processor.</param>
        public MessageReceiver(IReceiveMessages<T> messageReceiver, IProcessMessages<T> messageProcessor)
        {
            Verify.NotNull(messageReceiver, "messageReceiver");
            Verify.NotNull(messageProcessor, "messageProcessor");

            this.messageReceiver = messageReceiver;
            this.messageProcessor = messageProcessor;
            this.receiverTask = Task.Factory.StartNew(ReceiveAllMessages, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="MessageReceiver"/> class.
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
        /// Receive all messages from the underlying message bus.
        /// </summary>
        private void ReceiveAllMessages()
        {
            Message<T> message;
            while ((message = messageReceiver.Receive()) != null)
            {
                Log.TraceFormat("Message received: {0}", message);

                ProcessMessage(message);
            }
        }

        /// <summary>
        /// Process the received message.
        /// </summary>
        /// <param name="message">The message instance to process.</param>
        private async void ProcessMessage(Message<T> message)
        {
            try
            {
                await messageProcessor.ProcessAsync(message);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}
