using System;
using System.Collections.Concurrent;
using System.Threading;
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
    /// An in memory message bus based on a <see cref="BlockingCollection{T}"/> for use by single-process applications.
    /// </summary>
    public abstract class BlockingCollectionMessageBus
    {
        protected static readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of <see cref="BlockingCollectionMessageBus"/>.
        /// </summary>
        internal BlockingCollectionMessageBus()
        { }
    }

    /// <summary>
    /// An in memory message bus based on a <see cref="BlockingCollection{T}"/> for use by single-process applications.
    /// </summary>
    public class BlockingCollectionMessageBus<T> : BlockingCollectionMessageBus, ISendMessages<T>, IReceiveMessages<T>, IDisposable
    {
        private readonly BlockingCollection<Message<T>> messageQueue = new BlockingCollection<Message<T>>();
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent drained = new ManualResetEvent(false);
        private Boolean disposed;

        /// <summary>
        /// Gets the bounded capacity of this <see cref="BlockingCollectionMessageBus{T}"/>.
        /// </summary>
        public Int32 BoundedCapacity { get { return messageQueue.BoundedCapacity; } }

        /// <summary>
        /// Gets the number of items queued in this <see cref="BlockingCollectionMessageBus{T}"/>.
        /// </summary>
        public Int32 Count { get { return disposed ? 0 : messageQueue.Count; } }

        /// <summary>
        /// Initializes a new instance of <see cref="BlockingCollectionMessageBus{T}"/>.
        /// </summary>
        public BlockingCollectionMessageBus()
        {
            messageQueue = new BlockingCollection<Message<T>>();
            tokenSource = new CancellationTokenSource();
            drained = new ManualResetEvent(false);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BlockingCollectionMessageBus{T}"/> with the specified <paramref name="boundedCapacity"/>.
        /// </summary>
        /// <param name="boundedCapacity">The bounded size of the message bus.</param>
        public BlockingCollectionMessageBus(Int32 boundedCapacity)
        {
            Verify.GreaterThan(0, boundedCapacity, "boundedCapacity");

            messageQueue = new BlockingCollection<Message<T>>(boundedCapacity);
            tokenSource = new CancellationTokenSource();
            drained = new ManualResetEvent(false);
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="DefaultDiagnosticContext"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            Log.TraceFormat("Disposing");

            messageQueue.CompleteAdding();

            if (messageQueue.IsCompleted)
                drained.Set();

            Log.TraceFormat("Waiting for message queue drain");
            drained.WaitOne();

            tokenSource.Cancel();
            tokenSource.Dispose();
            messageQueue.Dispose();
            drained.Dispose();

            Log.TraceFormat("Disposing");
            disposed = true;

            Log.TraceFormat("Disposed");
        }

        /// <summary>
        /// Publishes a message on the underlying message bus.
        /// </summary>
        /// <param name="message">The message to publish on the underlying message bus.</param>
        public void Send(Message<T> message)
        {
            Verify.NotDisposed(this, disposed);
            Verify.NotNull(message, "message");

            Log.TraceFormat("Sending message {0}", message.Id);

            messageQueue.Add(message);
        }

        /// <summary>
        /// Blocks until a message is available or the bus has been disposed.
        /// </summary>
        /// <returns>The message received or null.</returns>
        public Message<T> Receive()
        {
            Message<T> message;

            if (disposed)
                return null;

            if (messageQueue.IsCompleted)
                drained.Set();

            Log.TraceFormat("Waiting for message");
            if (messageQueue.TryTake(out message, Timeout.Infinite, tokenSource.Token) && message != null)
                Log.TraceFormat("Received message {0}", message.Id);

            return message;
        }
    }
}
