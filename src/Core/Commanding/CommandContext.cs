using System;
using System.Collections.Generic;
using System.Threading;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.Eventing;
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

namespace Spark.Infrastructure.Commanding
{
    /// <summary>
    /// The command context wrapper used when invoking a command on an aggregate.
    /// </summary>
    public sealed class CommandContext : IDisposable
    {
        [ThreadStatic]
        private static CommandContext currentContext;
        private readonly CommandContext originalContext;
        private readonly IList<Event> raisedEvents;
        private readonly HeaderCollection headers;
        private readonly CommandEnvelope envelope;
        private readonly Guid commandId;
        private readonly Thread thread;
        private Boolean disposed;

        /// <summary>
        /// The current <see cref="CommandContext"/> if exists or null if no command context.
        /// </summary>
        public static CommandContext Current { get { return currentContext; } }

        /// <summary>
        /// The message header collection associated with this <see cref="CommandContext"/>.
        /// </summary>
        public HeaderCollection Headers { get { return headers; } }

        /// <summary>
        /// The unique <see cref="Command"/> message id associated with this <see cref="CommandContext"/>.
        /// </summary>
        public Guid CommandId { get { return commandId; } }

        /// <summary>
        /// The unique <see cref="Aggregate"/> identifier that will handle the associated <see cref="Command"/>.
        /// </summary>
        public Guid AggregateId { get { return envelope.AggregateId; } }

        /// <summary>
        /// The <see cref="Command"/> associated with this <see cref="CommandContext"/>.
        /// </summary>
        public Command Command { get { return envelope.Command; } }

        /// <summary>
        /// Initializes a new instance of <see cref="CommandContext"/> with the specified <paramref name="commandId"/> and <paramref name="headers"/>.
        /// </summary>
        /// <param name="commandId">The unique <see cref="Command"/> identifier.</param>
        /// <param name="headers">The <see cref="Command"/> headers.</param>
        /// <param name="envelope">The <see cref="CommandEnvelope"/> containing the target <see cref="Aggregate"/> identifier and <see cref="Command"/> to process.</param>
        public CommandContext(Guid commandId, HeaderCollection headers, CommandEnvelope envelope)
        {
            Verify.NotEqual(Guid.Empty, commandId, "commandId");
            Verify.NotNull(envelope, "envelope");
            Verify.NotNull(headers, "headers");

            this.raisedEvents = new List<Event>();
            this.originalContext = currentContext;
            this.thread = Thread.CurrentThread;
            this.commandId = commandId;
            this.envelope = envelope;
            this.headers = headers;

            currentContext = this;
        }

        /// <summary>
        /// Releases all unmanaged resources used by the current instance of the <see cref="CommandContext"/> class.
        /// </summary>
        ~CommandContext()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="CommandContext"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="CommandContext"/> class.
        /// </summary>
        private void Dispose(Boolean disposing)
        {
            if (!disposing || disposed)
                return;
           
            if (this.thread != Thread.CurrentThread)
                throw new InvalidOperationException(Exceptions.CommandContextInterleaved);

            if (this != Current)
                throw new InvalidOperationException(Exceptions.CommandContextInvalidThread);

            disposed = true;
            currentContext = originalContext;
        }

        /// <summary>
        /// Add the specified <see cref="Event"/> <paramref name="e"/> to the current <see cref="CommandContext"/>.
        /// </summary>
        /// <param name="e">The <see cref="Event"/> to be raise.</param>
        internal void Raise(Event e)
        {
            Verify.NotNull(e, "e");

            raisedEvents.Add(e);
        }

        /// <summary>
        /// Gets the set of <see cref="Event"/> instances raised within the current <see cref="CommandContext"/>.
        /// </summary>
        /// <returns></returns>
        internal EventCollection GetRaisedEvents()
        {
            return new EventCollection(raisedEvents);
        }
        
        /// <summary>
        /// Returns the <see cref="CommandContext"/> description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} - {1}", Command.GetType(), AggregateId);
        }
    }
}
