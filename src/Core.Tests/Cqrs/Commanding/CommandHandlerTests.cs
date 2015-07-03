using System;
using JetBrains.Annotations;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing;
using Spark.Messaging;
using Spark.Resources;
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

namespace Test.Spark.Cqrs.Commanding
{
    namespace UsingCommandHandler
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
            public void VerifyInitializedIfExplictCreateRequired()
            {
                var aggregate = new FakeAggregate(explicitCreateRequired: true, version: 0);
                var envelope = new CommandEnvelope(GuidStrategy.NewGuid(), new FakeCommand());
                var commandHandler = new CommandHandler(typeof(FakeAggregate), typeof(FakeCommand), AggregateStore.Object, (a, c) => ((FakeAggregate)a).Handle((FakeCommand)c));

                AggregateStore.Setup(mock => mock.Get(typeof(FakeAggregate), envelope.AggregateId)).Returns(aggregate);

                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope))
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => commandHandler.Handle(context));

                    Assert.Equal(Exceptions.AggregateNotInitialized.FormatWith(typeof(FakeAggregate), Guid.Empty), ex.Message);
                }
            }

            [Fact]
            public void VerifyInitializedIfExplictCreateRequiredAndCommandExceptionNotDefined()
            {
                var aggregate = new FakeAggregateWithExplicitCreate();
                var envelope = new CommandEnvelope(GuidStrategy.NewGuid(), new FakeCommand());
                var commandHandler = new CommandHandler(typeof(FakeAggregateWithExplicitCreate), typeof(FakeCommand), AggregateStore.Object, (a, c) => ((FakeAggregateWithExplicitCreate)a).Handle((FakeCommand)c));

                AggregateStore.Setup(mock => mock.Get(typeof(FakeAggregateWithExplicitCreate), envelope.AggregateId)).Returns(aggregate);

                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope))
                {
                    commandHandler.Handle(context);

                    Assert.True(aggregate.Handled);
                }
            }

            [Fact]
            public void DoNotVerifyInitializedIfImplicitCreateAllowed()
            {
                var aggregate = new FakeAggregate(explicitCreateRequired: false, version: 0);
                var envelope = new CommandEnvelope(GuidStrategy.NewGuid(), new FakeCommand());
                var commandHandler = new CommandHandler(typeof(FakeAggregate), typeof(FakeCommand), AggregateStore.Object, (a, c) => ((FakeAggregate)a).Handle((FakeCommand)c));

                AggregateStore.Setup(mock => mock.Get(typeof(FakeAggregate), envelope.AggregateId)).Returns(aggregate);

                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope))
                {
                    commandHandler.Handle(context);

                    Assert.True(aggregate.Handled);
                }
            }

            [Fact]
            public void SaveAggregateOnSuccessIfEventsRaised()
            {
                var aggregate = new FakeAggregate();
                var envelope = new CommandEnvelope(GuidStrategy.NewGuid(), new FakeCommand());
                var commandHandler = new CommandHandler(typeof(FakeAggregate), typeof(FakeCommand), AggregateStore.Object, (a, c) => ((FakeAggregate)a).Handle((FakeCommand)c));

                AggregateStore.Setup(mock => mock.Get(typeof(FakeAggregate), envelope.AggregateId)).Returns(aggregate);

                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope))
                    commandHandler.Handle(context);

                AggregateStore.Verify(mock => mock.Save(aggregate, It.IsAny<CommandContext>()), Times.Once());
            }

            [Fact]
            public void DoNotSaveAggregateOnSuccessIfNoEventsRaised()
            {
                var aggregate = new FakeAggregate();
                var envelope = new CommandEnvelope(GuidStrategy.NewGuid(), new FakeCommand());
                var commandHandler = new CommandHandler(typeof(FakeAggregate), typeof(FakeCommand), AggregateStore.Object, (a, c) => { });

                AggregateStore.Setup(mock => mock.Get(typeof(FakeAggregate), envelope.AggregateId)).Returns(aggregate);

                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope))
                    commandHandler.Handle(context);

                AggregateStore.Verify(mock => mock.Save(aggregate, It.IsAny<CommandContext>()), Times.Never);
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

        internal class FakeAggregate : Aggregate
        {
            private readonly Boolean explicitCreateRequired;

            public Boolean Handled { get; private set; }
            protected override bool RequiresExplicitCreate { get { return explicitCreateRequired; } }

            public FakeAggregate()
                : this(false, 0)
            { }

            public FakeAggregate(Boolean explicitCreateRequired, Int32 version)
            {
                this.explicitCreateRequired = explicitCreateRequired;
                Version = version;
            }

            [UsedImplicitly]
            public void Handle(FakeCommand command)
            {
                Raise(new FakeEvent());
                Handled = true;
            }
        }

        internal class FakeAggregateWithExplicitCreate : Aggregate
        {
            public Boolean Handled { get; private set; }

            protected override bool CanCreateAggregate(Command command)
            {
                return command is FakeCommand;
            }

            [UsedImplicitly]
            public void Handle(FakeCommand command)
            {
                Raise(new FakeEvent());
                Handled = true;
            }
        }

        internal class FakeCommand : Command
        { }

        internal class FakeEvent : Event
        { }
    }
}
