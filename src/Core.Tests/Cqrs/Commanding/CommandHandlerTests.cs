using System;
using JetBrains.Annotations;
using Moq;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.EventStore;
using Spark.Messaging;
using Xunit;

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

namespace Spark.Tests.Cqrs.Commanding
{
    public static class UsingCommandHandler
    {
        public class WhenCreatingNewHandler
        {
            [Fact]
            public void AggregateTypeCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandHandler(null, typeof(FakeCommand), new Mock<IStoreAggregates>().Object, (a, c) => { }));

                Assert.Equal("aggregateType", ex.ParamName);
            }

            [Fact]
            public void AggregateTypeMustBeAnAggregateType()
            {
                var ex = Assert.Throws<ArgumentException>(() => new CommandHandler(typeof(Object), typeof(FakeCommand), new Mock<IStoreAggregates>().Object, (a, c) => { }));

                Assert.Equal("aggregateType", ex.ParamName);
            }

            [Fact]
            public void CommandTypeCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandHandler(typeof(FakeAggregate), null, new Mock<IStoreAggregates>().Object, (a, c) => { }));

                Assert.Equal("commandType", ex.ParamName);
            }

            [Fact]
            public void CommandTypeMustBeAnAggregateType()
            {
                var ex = Assert.Throws<ArgumentException>(() => new CommandHandler(typeof(FakeAggregate), typeof(Object), new Mock<IStoreAggregates>().Object, (a, c) => { }));

                Assert.Equal("commandType", ex.ParamName);
            }

            [Fact]
            public void AggregateStoreCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandHandler(typeof(FakeAggregate), typeof(FakeCommand), null, (a, c) => { }));

                Assert.Equal("aggregateStore", ex.ParamName);
            }

            [Fact]
            public void ExecutorCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandHandler(typeof(FakeAggregate), typeof(FakeCommand), new Mock<IStoreAggregates>().Object, null));

                Assert.Equal("executor", ex.ParamName);
            }
        }


        // ReSharper disable AccessToDisposedClosure
        public class WhenHandlingCommandContext
        {
            protected readonly Mock<IStoreAggregates> AggregateStore = new Mock<IStoreAggregates>();

            [Fact]
            public void SaveAggregateOnSuccess()
            {
                var aggregate = new FakeAggregate();
                var envelope = new CommandEnvelope(GuidStrategy.NewGuid(), new FakeCommand());
                var commandHandler = new CommandHandler(typeof(FakeAggregate), typeof(FakeCommand), AggregateStore.Object, (a, c) => { });

                AggregateStore.Setup(mock => mock.Get(typeof(FakeAggregate), envelope.AggregateId)).Returns(aggregate);

                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope))
                    commandHandler.Handle(context);

                AggregateStore.Verify(mock => mock.Save(aggregate, It.IsAny<CommandContext>()), Times.Once());
            }
        }
        // ReSharper restore AccessToDisposedClosure

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnFriendlyDescription()
            {
                var command = new CommandHandler(typeof(FakeAggregate), typeof(FakeCommand), new Mock<IStoreAggregates>().Object, (a, c) => { });

                Assert.Equal(String.Format("{0} Command Handler ({1})", typeof(FakeCommand), typeof(FakeAggregate)), command.ToString());
            }
        }

        private class FakeAggregate : Aggregate
        { }

        [UsedImplicitly]
        private class FakeCommand : Command
        { }
    }
}
