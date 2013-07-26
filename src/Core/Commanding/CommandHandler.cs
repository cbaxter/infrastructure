using System;
using Spark.Domain;
using Spark.Logging;

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
    /// Represents an <see cref="Aggregate"/> command handler method executor.
    /// </summary>
    public sealed class CommandHandler
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly Action<Aggregate, Command> executor;
        private readonly IStoreAggregates aggregateStore;
        private readonly Type aggregateType;
        private readonly Type commandType;

        /// <summary>
        /// The aggregate <see cref="Type"/> associated with this command handler executor.
        /// </summary>
        public Type AggregateType { get { return aggregateType; } }

        /// <summary>
        /// The aggregate <see cref="Type"/> associated with this command handler executor.
        /// </summary>
        public Type CommandType { get { return commandType; } }

        /// <summary>
        /// Initializes a new instance of <see cref="CommandHandler"/>.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        /// <param name="commandType">The command type.</param>
        /// <param name="aggregateStore">The aggregate store.</param>
        /// <param name="executor">The command handler executor.</param>
        public CommandHandler(Type aggregateType, Type commandType, IStoreAggregates aggregateStore, Action<Aggregate, Command> executor)
        {
            Verify.NotNull(executor, "executor");
            Verify.NotNull(commandType, "commandType");
            Verify.NotNull(aggregateType, "aggregateType");
            Verify.NotNull(aggregateStore, "aggregateStore");
            Verify.TypeDerivesFrom(typeof(Command), commandType, "commandType");
            Verify.TypeDerivesFrom(typeof(Aggregate), aggregateType, "aggregateType");

            this.aggregateStore = aggregateStore;
            this.aggregateType = aggregateType;
            this.commandType = commandType;
            this.executor = executor;
        }

        /// <summary>
        /// Invokes the underlying <see cref="Aggregate"/> command handler method for <see cref="Command"/>.
        /// </summary>
        /// <param name="context">The current command context.</param>
        public void Handle(CommandContext context)
        {
            var aggregate = aggregateStore.Get(AggregateType, context.AggregateId);

            Log.DebugFormat("Executing {0} command on aggregate {1}", context.Command, aggregate);

            executor(aggregate, context.Command);

            Log.Trace("Saving aggregate state");

            aggregateStore.Save(aggregate, context);

            Log.Trace("Aggregate state saved");
        }

        /// <summary>
        /// Returns the <see cref="CommandHandler"/> description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} Command Handler ({1})", CommandType, AggregateType);
        }
    }
}
