using System;
using JetBrains.Annotations;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing;
using Spark.Data;
using Spark.Messaging;
using Test.Spark.Configuration;
using Xunit;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Test.Spark.Cqrs.Commanding
{
    namespace UsingCommandProcessor
    {
        public abstract class UsingCommandProcessorBase
        {
            internal readonly Mock<IDetectTransientErrors> TransientErrorRegistry = new Mock<IDetectTransientErrors>();
            protected readonly Mock<IRetrieveCommandHandlers> HandlerRegistry = new Mock<IRetrieveCommandHandlers>();
            protected readonly Mock<IStoreAggregates> AggregateStore = new Mock<IStoreAggregates>();
            protected readonly CommandProcessor Processor;

            protected UsingCommandProcessorBase()
            {
                TransientErrorRegistry.Setup(mock => mock.IsTransient(It.IsAny<ConcurrencyException>())).Returns(true);
                Processor = new CommandProcessor(HandlerRegistry.Object, TransientErrorRegistry.Object, new CommandProcessorSettings());
            }
        }

        public class WhenCreatingNewProcessor
        {
            [Fact]
            public void CommandHandlerRegistryCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new CommandProcessor(null, new IDetectTransientErrors[0]));

                Assert.Equal("commandHandlerRegistry", ex.ParamName);
            }
        }

        public class WhenProcessingCommands : UsingCommandProcessorBase
        {
            [Fact]
            public void RetrieveCommandHandlerBasedOnCommandType()
            {
                var command = new FakeCommand();
                var aggregate = new FakeAggregate();
                var envelope = new CommandEnvelope(GuidStrategy.NewGuid(), command);
                var message = Message.Create(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope);

                HandlerRegistry.Setup(mock => mock.GetHandlerFor(command)).Returns(new CommandHandler(typeof(FakeAggregate), typeof(FakeCommand), AggregateStore.Object, (a, c) => { }));
                AggregateStore.Setup(mock => mock.Get(typeof(FakeAggregate), envelope.AggregateId)).Returns(aggregate);

                Processor.Process(message);

                HandlerRegistry.Verify(mock => mock.GetHandlerFor(command), Times.Once());
            }

            [Fact]
            public void ReloadAggregateOnConcurrencyException()
            {
                var save = 0;
                var command = new FakeCommand();
                var aggregate = new FakeAggregate();
                var envelope = new CommandEnvelope(GuidStrategy.NewGuid(), command);
                var message = Message.Create(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope);
                var ex = new ConcurrencyException();

                HandlerRegistry.Setup(mock => mock.GetHandlerFor(command)).Returns(new CommandHandler(typeof(FakeAggregate), typeof(FakeCommand), AggregateStore.Object, (a, c) => ((FakeAggregate)a).Handle((FakeCommand)c)));
                AggregateStore.Setup(mock => mock.Get(typeof(FakeAggregate), envelope.AggregateId)).Returns(aggregate);
                AggregateStore.Setup(mock => mock.Save(aggregate, It.IsAny<CommandContext>())).Callback(() =>
                {
                    if (++save == 1)
                        throw ex;
                });

                Processor.Process(message);

                AggregateStore.Verify(mock => mock.Get(typeof(FakeAggregate), envelope.AggregateId), Times.Exactly(2));
            }

            [Fact]
            public void WillTimeoutEventuallyIfCannotSave()
            {
                var command = new FakeCommand();
                var aggregate = new FakeAggregate();
                var envelope = new CommandEnvelope(GuidStrategy.NewGuid(), command);
                var message = Message.Create(GuidStrategy.NewGuid(), HeaderCollection.Empty, envelope);
                var processor = new CommandProcessor(HandlerRegistry.Object, TransientErrorRegistry.Object, new CommandProcessorSettings { RetryTimeout = TimeSpan.FromMilliseconds(20) });

                SystemTime.ClearOverride();

                AggregateStore.Setup(mock => mock.Get(typeof(FakeAggregate), envelope.AggregateId)).Returns(aggregate);
                AggregateStore.Setup(mock => mock.Save(aggregate, It.IsAny<CommandContext>())).Callback(() => { throw new ConcurrencyException(); });
                HandlerRegistry.Setup(mock => mock.GetHandlerFor(command)).Returns(new CommandHandler(typeof(FakeAggregate), typeof(FakeCommand), AggregateStore.Object, (a, c) => ((FakeAggregate)a).Handle((FakeCommand)c)));

                Assert.Throws<TimeoutException>(() => processor.Process(message));
            }
        }

        internal class FakeAggregate : Aggregate
        {
            protected override bool RequiresExplicitCreate { get { return false; } }

            [UsedImplicitly]
            public void Handle(FakeCommand command)
            {
                Raise(new FakeEvent());
            }
        }

        internal class FakeCommand : Command
        { }

        internal class FakeEvent : Event
        { }
    }
}
