using System;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Principal;
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
    /// Base class for all command messages.
    /// </summary>
    public abstract class Command
    {
        /// <summary>
        /// The target aggregate id for this <see cref="Command"/>.
        /// </summary>
        [IgnoreDataMember]
        internal Guid AggregateId { get { return GetAggregateId(); } }
        //TODO: Replace with --> internal protected abstract Guid AggregateId { get; } (should simplify inherited class code)
        //      OR
        //      remove completely and create command wrapper class (envelop) that separates aggregate id from command/event...
        //      message: { id: ~, headers: ~, payload: { aggregateId, command } so wrap command in CommandEnvelope (or EventEnvelop) that attaches additional meta-data?
        //      commandPublisher(aggregateId, command) ==> messageFactory.Create(new CommandEnvelope(aggregateId, command));
        //      eventDispatcher(aggregateId, event, headers) ==> messageFactory.Create(new EventEnvelope(aggregateId, aggregateType, event), headers);
        //      envelope classes would be internal??? classes could actually be structs?
        //      OR
        //      Convert existing ICreateMessages in to something else and have explicit ICreateCommandMessages, ICreateEventMessages... 

        /// <summary>
        /// The message header collection associated with this command instance.
        /// </summary>
        [IgnoreDataMember]
        public HeaderCollection Headers
        {
            get
            {
                var context = CommandContext.Current;
                
                return context == null ? HeaderCollection.Empty : context.Headers;
            }
        }

        /// <summary>
        /// Gets the aggregate id mapping for this <see cref="Command"/> instance.
        /// </summary>
        protected abstract Guid GetAggregateId();

        /// <summary>
        /// Returns the origin server name that published the command or an empty string if not set.
        /// </summary>
        public String GetOrigin()
        {
            return Headers.GetOrigin();
        }

        /// <summary>
        /// Returns the timestamp of when the command was published or the current system time if not set.
        /// </summary>
        public DateTime GetTimestamp()
        {
            return Headers.GetTimestamp();
        }

        /// <summary>
        /// Returns the command publisher's remote address or <see cref="IPAddress.None"/> if not set.
        /// </summary>
        public IPAddress GetRemoteAddress()
        {
            return Headers.GetRemoteAddress();
        }

        /// <summary>
        /// Returns the command publisher's client address or <see cref="IPAddress.None"/> if not set.
        /// </summary>
        /// <remarks>Value will be the same as <see cref="GetRemoteAddress"/> unless the request went through an intermediary such as a load-balancer or proxy.</remarks>
        public IPAddress GetUserAddress()
        {
            return Headers.GetUserAddress();
        }
        
        /// <summary>
        /// Returns the <see cref="IIdentity.Name"/> of the user principal that published this command, or <see cref="String.Empty"/> if not set.
        /// </summary>
        public String GetUserName()
        {
            return Headers.GetUserName();
        }

        /// <summary>
        /// Returns the <see cref="Command"/> description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} - {1}", GetType(), AggregateId);
        }
    }
}
