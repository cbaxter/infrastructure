using System;
using System.Collections.Generic;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.Logging;
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
        /// Publishes the specified <paramref name="command"/> on the underlying message bus.
        /// </summary>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="command"/>.</param>
        /// <param name="command">The command to be published.</param>
        /// <param name="headers">The set of message headers associated with the command.</param>
        public void Publish(Guid aggregateId, Command command, IEnumerable<Header> headers)
        {
            Verify.NotNull(command, "command");

            Log.TraceFormat("Publishing {0} to {1}", command, aggregateId);

            messageSender.Send(messageFactory.Create(headers, new CommandEnvelope(aggregateId, command)));
        }
    }
}
