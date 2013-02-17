using System;
using Spark.Infrastructure.Domain;

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
    /// Represents an <see cref="Aggregate"/> command handler method executor.
    /// </summary>
    public sealed class CommandHandler
    {
        private readonly Action<Aggregate, Command> executor;
        private readonly Type aggregateType;

        /// <summary>
        /// The aggregate <see cref="Type"/> associated with this command handler executor.
        /// </summary>
        public Type AggregateType { get { return aggregateType; } }

        /// <summary>
        /// Initializes a new instance of <see cref="CommandHandler"/>.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        /// <param name="executor">The command handler executor.</param>
        public CommandHandler(Type aggregateType, Action<Aggregate, Command> executor)
        {
            Verify.NotNull(executor, "executor");
            Verify.NotNull(aggregateType, "aggregateType");
            Verify.TypeDerivesFrom(typeof(Aggregate), aggregateType, "aggregateType");

            this.aggregateType = aggregateType;
            this.executor = executor;
        }

        /// <summary>
        /// Invokes the underlying <see cref="Aggregate"/> command handler method for <see cref="Command"/>.
        /// </summary>
        /// <param name="aggregate"></param>
        /// <param name="command"></param>
        public void Handle(Aggregate aggregate, Command command)
        {
            Verify.NotNull(aggregate, "aggregate");
            Verify.NotNull(command, "command");

            executor.Invoke(aggregate, command);
        }

        /// <summary>
        /// Returns the <see cref="CommandHandler"/> description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} Command Handler", AggregateType);
        }
    }
}
