using System.Collections.Generic;
using Spark.Logging;
using Spark.Messaging;

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

namespace Spark.Commanding
{
    /// <summary>
    /// Publishes commands on the underlying <see cref="Command"/> message bus.
    /// </summary>
    public sealed class CommandPublisher : IPublishCommands
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly ISendMessages<CommandEnvelope> messageSender;
        private readonly ICreateMessages messageFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="CommandPublisher"/>.
        /// </summary>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="messageSender">The message sender.</param>
        public CommandPublisher(ICreateMessages messageFactory, ISendMessages<CommandEnvelope> messageSender)
        {
            Verify.NotNull(messageFactory, "messageFactory");
            Verify.NotNull(messageSender, "messageSender");

            this.messageFactory = messageFactory;
            this.messageSender = messageSender;
        }

        /// <summary>
        /// Publishes the specified <paramref name="payload"/> on the underlying message bus.
        /// </summary>
        /// <param name="headers">The set of message headers associated with the command.</param>
        /// <param name="payload">The command payload to be published.</param>
        public void Publish(IEnumerable<Header> headers, CommandEnvelope payload)
        {
            Verify.NotNull(payload, "payload");

            Log.TraceFormat("Publishing {0} to {1}", payload.Command, payload.AggregateId);

            messageSender.Send(messageFactory.Create(headers, payload));
        }
    }
}
