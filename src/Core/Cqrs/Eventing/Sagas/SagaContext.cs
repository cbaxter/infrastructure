using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Messaging;
using Spark.Resources;

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

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// The saga context wrapper used when handling an event within an saga.
    /// </summary>
    public sealed class SagaContext : IDisposable
    {
        [ThreadStatic]
        private static SagaContext currentContext;
        private readonly SagaContext originalContext;
        private readonly Thread thread;
        private readonly Event @event;
        private readonly Type sagaType;
        private readonly Guid sagaId;
        private IList<SagaCommand> publishedCommands;
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
        /// Indicates if the <see cref="Saga.Timeout"/> associated with this <see cref="SagaContext"/> has changed.
        /// </summary>
        public Boolean TimeoutChanged { get; internal set; }

        /// <summary>
        /// The <see cref="Event"/> associated with this <see cref="SagaContext"/>.
        /// </summary>
        public Event Event { get { return @event; } }

        /// <summary>
        /// Initalizes a new instance of <see cref="SagaContext"/> with the specified <paramref name="sagaType"/> and <paramref name="sagaId"/>.
        /// </summary>
        /// <param name="sagaType">The underlying saga <see cref="Type"/> associated with this <see cref="SagaContext"/>.</param>
        /// <param name="sagaId">The <see cref="Saga"/> correlation id associated with this <see cref="SagaContext"/>.</param>
        /// <param name="e">The <see cref="Event"/>.</param>
        public SagaContext(Type sagaType, Guid sagaId, Event e)
        {
            Verify.NotNull(sagaType, "sagaType");
            Verify.NotNull(e, "e");

            this.originalContext = currentContext;
            this.thread = Thread.CurrentThread;
            this.sagaType = sagaType;
            this.sagaId = sagaId;
            this.@event = e;

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
        /// Publishes the specified <paramref name="command"/> with the enumerable set of custom message headers.
        /// </summary>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="command"/>.</param>
        /// <param name="command">The command to be published.</param>
        /// <param name="headers">The set of message headers associated with the command.</param>
        internal void Publish(Guid aggregateId, IEnumerable<Header> headers, Command command)
        {
            if (publishedCommands == null)
                publishedCommands = new List<SagaCommand>();

            publishedCommands.Add(new SagaCommand(aggregateId, headers, command));
        }

        /// <summary>
        /// Gets the set of <see cref="Message{CommandEnvelope}"/> instances published within the current <see cref="SagaContext"/>.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SagaCommand> GetPublishedCommands()
        {
            return publishedCommands ?? Enumerable.Empty<SagaCommand>();
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
