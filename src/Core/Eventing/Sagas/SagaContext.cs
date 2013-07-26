using System;
using System.Collections.Generic;
using System.Threading;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Messaging;
using Spark.Infrastructure.Resources;

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

namespace Spark.Infrastructure.Eventing.Sagas
{
    /// <summary>
    /// The saga context wrapper used when handling an event within an saga.
    /// </summary>
    public sealed class SagaContext : IDisposable
    {
        [ThreadStatic]
        private static SagaContext currentContext;
        private readonly SagaContext originalContext;
        private readonly IList<Message<CommandEnvelope>> publishedCommands;
        private readonly Thread thread;
        private readonly Type sagaType;
        private readonly Guid sagaId;
        private Boolean disposed;

        /// <summary>
        /// The current <see cref="SagaContext"/> if exists or null if no saga context.
        /// </summary>
        public static SagaContext Current { get { return currentContext; } }

        /// <summary>
        /// The <see cref="Saga"/> correlation id associated with this <see cref="SagaContext"/>.
        /// </summary>
        public Guid SagaId { get { return sagaId; } }

        /// <summary>
        /// The underlying saga <see cref="Type"/> associated with this <see cref="SagaContext"/>.
        /// </summary>
        public Type SagaType { get { return sagaType; } }
        
        /// <summary>
        /// Initalizes a new instance of <see cref="SagaContext"/> with the specified <paramref name="sagaType"/> and <paramref name="sagaId"/>.
        /// </summary>
        /// <param name="sagaType">The underlying saga <see cref="Type"/> associated with this <see cref="SagaContext"/>.</param>
        /// <param name="sagaId">The <see cref="Saga"/> correlation id associated with this <see cref="SagaContext"/>.</param>
        public SagaContext(Type sagaType, Guid sagaId)
        {
            this.publishedCommands = new List<Message<CommandEnvelope>>();
            this.originalContext = currentContext;
            this.thread = Thread.CurrentThread;
            this.sagaType = sagaType;
            this.sagaId = sagaId;

            currentContext = this;
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="SagaContext"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            if (this.thread != Thread.CurrentThread)
                throw new InvalidOperationException(Exceptions.SagaContextInterleaved);

            if (this != Current)
                throw new InvalidOperationException(Exceptions.SagaContextInvalidThread);

            disposed = true;
            currentContext = originalContext;
        }

        /// <summary>
        /// Publishes the specified <paramref name="message"/> on the underlying message bus after successful save.
        /// </summary>
        /// <param name="message">The message to be published.</param>
        internal void Publish(Message<CommandEnvelope> message)
        {
            Verify.NotNull(message, "message");

            publishedCommands.Add(message);
        }

        /// <summary>
        /// Gets the set of <see cref="Message{CommandEnvelope}"/> instances published within the current <see cref="SagaContext"/>.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<Message<CommandEnvelope>> GetPublishedCommands()
        {
            return publishedCommands;
        }

        /// <summary>
        /// Returns the <see cref="SagaContext"/> description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} - {1}", SagaType, SagaId);
        }
    }
}
