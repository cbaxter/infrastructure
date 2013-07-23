using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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

namespace Spark.Infrastructure.Messaging
{
    /// <summary>
    /// A message envelope containing a unique identifier, message headers and associated payload.
    /// </summary>
    public static class Message
    {
        /// <summary>
        /// Creates a new instance of <see cref="Message{TPayload}"/>.
        /// </summary>
        /// <param name="id">The unique message identifier</param>
        /// <param name="headers">The set of headers associated with this message.</param>
        /// <param name="payload">The message payload.</param>
        public static Message<TPayload> Create<TPayload>(Guid id, HeaderCollection headers, TPayload payload)
        {
            return new Message<TPayload>(id, headers, payload);
        }
    }

    /// <summary>
    /// A message envelope containing a unique identifier, message headers and associated payload.
    /// </summary>
    [Serializable]
    public sealed class Message<TPayload> : IMessage
    {
        private readonly Guid id;
        private readonly HeaderCollection headers;
        private readonly TPayload payload;

        /// <summary>
        /// The unique message identifier.
        /// </summary>
        public Guid Id { get { return id; } }

        /// <summary>
        /// The set of message headers for this message.
        /// </summary>
        public HeaderCollection Headers { get { return headers; } }

        /// <summary>
        /// The message payload.
        /// </summary>
        public TPayload Payload { get { return payload; } }
        Object IMessage.Payload { get { return payload; } }

        /// <summary>
        /// Initializes a new instance of <see cref="Message{TPayload}"/>.
        /// </summary>
        /// <param name="id">The unique message identifier</param>
        /// <param name="headers">The set of headers associated with this message.</param>
        /// <param name="payload">The message payload.</param>
        public Message(Guid id, HeaderCollection headers, TPayload payload)
        {
            Verify.NotEqual(Guid.Empty, id, "id");
            Verify.NotNull(headers, "headers");
            Verify.NotNull((Object)payload, "payload");

            this.id = id;
            this.headers = headers;
            this.payload = payload;
        }

        /// <summary>
        /// Returns the description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} - {1}", Id, Payload);
        }
    }
}
