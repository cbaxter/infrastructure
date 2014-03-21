using System;
using Spark.Cqrs.Domain;

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

namespace Spark.Cqrs.Commanding
{
    /// <summary>
    /// The command message envelope that pairs a <see cref="Command"/> with the target <see cref="Aggregate"/> identifier.
    /// </summary>
    [Serializable]
    public sealed class CommandEnvelope
    {
        private class NullCommand : Command { }
        private readonly Guid aggregateId;
        private readonly Command command;

        /// <summary>
        /// Represents an empty <see cref="CommandEnvelope"/>. This field is read-only.
        /// </summary>
        public static readonly CommandEnvelope Empty = new CommandEnvelope(Guid.Empty, new NullCommand());

        /// <summary>
        /// The unique <see cref="Aggregate"/> identifier that is the target of the associated <see cref="Command"/>.
        /// </summary>
        public Guid AggregateId { get { return aggregateId; } }

        /// <summary>
        /// The command payload that is to be processed by the specified <see cref="Aggregate"/>.
        /// </summary>
        public Command Command { get { return command; } }

        /// <summary>
        /// Creates a new instance of <see cref="CommandEnvelope"/> for the specified <paramref name="aggregateId"/>and <paramref name="command"/>.
        /// </summary>
        /// <param name="aggregateId">The unique <see cref="Aggregate"/> identifier that is the target of the associated <paramref name="command"/>.</param>
        /// <param name="command">The command payload that is to be processed by the specified <see cref="Aggregate"/>.</param>
        public CommandEnvelope(Guid aggregateId, Command command)
        {
            Verify.NotNull(command, "command");

            this.aggregateId = aggregateId;
            this.command = command;
        }

        /// <summary>
        /// Returns the <see cref="CommandEnvelope"/> description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} - {1}", Command.GetType(), AggregateId);
        }
    }
}
