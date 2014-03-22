using System;
using System.Threading;
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

namespace Spark.Cqrs.Eventing
{
    /// <summary>
    /// The command context wrapper used when invoking a command on an aggregate.
    /// </summary>
    public sealed class EventContext : IDisposable
    {
        [ThreadStatic]
        private static EventContext currentContext;
        private readonly EventContext originalContext;
        private readonly HeaderCollection headers;
        private readonly Guid aggregateId;
        private readonly Thread thread;
        private readonly Event @event;
        private Boolean disposed;

        /// <summary>
        /// The current <see cref="EventContext"/> if exists or null if no command context.
        /// </summary>
        public static EventContext Current { get { return currentContext; } }

        /// <summary>
        /// The message header collection associated with this <see cref="EventContext"/>.
        /// </summary>
        public HeaderCollection Headers { get { return headers; } }

        /// <summary>
        /// The unique <see cref="Aggregate"/> id associated with this <see cref="EventContext"/>.
        /// </summary>
        public Guid AggregateId { get { return aggregateId; } }

        /// <summary>
        /// The <see cref="Event"/> associated with this <see cref="EventContext"/>.
        /// </summary>
        public Event Event { get { return @event; } }

        /// <summary>
        /// Initializes a new instance of <see cref="EventContext"/> with the specified <paramref name="aggregateId"/> and <paramref name="headers"/>.
        /// </summary>
        /// <param name="aggregateId">The unique <see cref="Aggregate"/> identifier.</param>
        /// <param name="headers">The <see cref="Event"/> headers.</param>
        /// <param name="e">The <see cref="Event"/>.</param>
        public EventContext(Guid aggregateId, HeaderCollection headers, Event e)
        {
            Verify.NotEqual(Guid.Empty, aggregateId, "aggregateId");
            Verify.NotNull(headers, "headers");
            Verify.NotNull(e, "e");

            this.originalContext = currentContext;
            this.thread = Thread.CurrentThread;
            this.aggregateId = aggregateId;
            this.headers = headers;
            this.@event = e;

            currentContext = this;
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="EventContext"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            if (thread != Thread.CurrentThread)
                throw new InvalidOperationException(Exceptions.EventContextInterleaved);

            if (this != Current)
                throw new InvalidOperationException(Exceptions.EventContextInvalidThread);

            disposed = true;
            currentContext = originalContext;
        }

        /// <summary>
        /// Returns the <see cref="EventContext"/> description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} - {1}", AggregateId, Event);
        }
    }
}
