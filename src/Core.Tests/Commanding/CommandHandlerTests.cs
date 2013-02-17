using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Domain;
using Xunit;

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

namespace Spark.Infrastructure.Tests.Commanding
{
    public static class UsingCommandHandler
    {
        public class WhenCreatingNewHandler
        {
            [Fact]
            public void AggregateTypeCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandHandler(null, (a, c) => { }));

                Assert.Equal("aggregateType", ex.ParamName);
            }

            [Fact]
            public void AggregateTypeMustBeAnAggregateType()
            {
                var ex = Assert.Throws<ArgumentException>(() => new CommandHandler(typeof(Object), (a, c) => { }));

                Assert.Equal("aggregateType", ex.ParamName);
            }

            [Fact]
            public void ExecutorCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandHandler(typeof(FakeAggregate), null));

                Assert.Equal("executor", ex.ParamName);
            }
        }

        public class WhenInvoking
        {
            [Fact]
            public void AggregateCannotBeNull()
            {
                var commandHandler = new CommandHandler(typeof (FakeAggregate), (a, c) => { });

                var ex = Assert.Throws<ArgumentNullException>(() => commandHandler.Handle(null, new FakeCommand()));

                Assert.Equal("aggregate", ex.ParamName);
            }

            [Fact]
            public void CommandCannotBeNull()
            {
                var commandHandler = new CommandHandler(typeof(FakeAggregate), (a, c) => { });

                var ex = Assert.Throws<ArgumentNullException>(() => commandHandler.Handle(new FakeAggregate(), null));

                Assert.Equal("command", ex.ParamName);
            }

            [Fact]
            public void InvokesUnderlyingAggregateCommandHandler()
            {
                var executed = false;
                var commandHandler = new CommandHandler(typeof(FakeAggregate), (a, c) => executed = true);

                commandHandler.Handle(new FakeAggregate(), new FakeCommand());

                Assert.True(executed);
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnFriendlyDescription()
            {
                var command = new CommandHandler(typeof(FakeAggregate), (a, c) => { });

                Assert.Equal(String.Format("{0} Command Handler", typeof(FakeAggregate)), command.ToString());
            }
        }

        private class FakeAggregate : Aggregate
        { }

        private class FakeCommand : Command
        {
            protected override Guid GetAggregateId()
            {
                return Guid.NewGuid();
            }
        }
    }
}
