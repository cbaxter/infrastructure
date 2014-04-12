using System;
using JetBrains.Annotations;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Domain.Mappings;
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
    namespace UsingCommandHandlerRegistry
    {
        public class WhenCreatingNewRegistry
        {
            private readonly Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
            private readonly Mock<IStoreAggregates> aggregateStore = new Mock<IStoreAggregates>();
            private readonly Mock<ILocateTypes> typeLocator = new Mock<ILocateTypes>();

            [Fact]
            public void NoAggregatesRequired()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new Type[0]);

                Assert.DoesNotThrow(() => new CommandHandlerRegistry(aggregateStore.Object, typeLocator.Object, serviceProvider.Object));
            }

            [Fact]
            public void ThrowMappingExceptionIfMoreThanOneMappingStrategyDefined()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(MultipleStrategiesAggregate) });

                var ex = Assert.Throws<MappingException>(() => new CommandHandlerRegistry(aggregateStore.Object, typeLocator.Object, serviceProvider.Object));

                Assert.Equal(Exceptions.AggregateHandleByStrategyAmbiguous.FormatWith(typeof(MultipleStrategiesAggregate)), ex.Message);
            }

            [Fact]
            public void ThrowMappingExceptionIfCommandHandledByMoreThanOneAggregate()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(ImplicitStrategyAggregate), typeof(ExplicitStrategyAggregate) });

                var ex = Assert.Throws<MappingException>(() => new CommandHandlerRegistry(aggregateStore.Object, typeLocator.Object, serviceProvider.Object));

                Assert.Equal(Exceptions.HandleMethodMustBeAssociatedWithSingleAggregate.FormatWith(typeof(ImplicitStrategyAggregate), typeof(FakeCommand)), ex.Message);
            }

            [Fact]
            public void DefaultMappingStrategyUsedWhenNoExplicitStrategyDefined()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(ImplicitStrategyAggregate) });

                var registry = new CommandHandlerRegistry(aggregateStore.Object, typeLocator.Object, serviceProvider.Object);
                var handler = registry.GetHandlerFor(new FakeCommand());

                Assert.NotNull(handler);
            }

            [Fact]
            public void CustomMappingStrategyUsedWhenExplicitStrategyDefined()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(ExplicitStrategyAggregate) });

                var registry = new CommandHandlerRegistry(aggregateStore.Object, typeLocator.Object, serviceProvider.Object);
                var handler = registry.GetHandlerFor(new FakeCommand());

                Assert.NotNull(handler);
            }

            [Fact]
            public void CanResolveRegisteredServices()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(ServicedAggregate) });
                serviceProvider.Setup(mock => mock.GetService(typeof(FakeService))).Returns(new FakeService());

                var registry = new CommandHandlerRegistry(aggregateStore.Object, typeLocator.Object, serviceProvider.Object);
                var handler = registry.GetHandlerFor(new FakeCommand());

                Assert.NotNull(handler);
            }

            [Fact]
            public void RegisteredServiceOnlyResolvedOncePerHandler()
            {
                var aggregateId = GuidStrategy.NewGuid();

                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(ServicedAggregate) });
                aggregateStore.Setup(mock => mock.Get(typeof(ServicedAggregate), aggregateId)).Returns(new ServicedAggregate());
                serviceProvider.Setup(mock => mock.GetService(typeof(FakeService))).Returns(new FakeService());

                var registry = new CommandHandlerRegistry(aggregateStore.Object, typeLocator.Object, serviceProvider.Object);
                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, new CommandEnvelope(aggregateId, new FakeCommand())))
                {
                    var handler1 = registry.GetHandlerFor(new FakeCommand());
                    handler1.Handle(context);
                    handler1.Handle(context);

                    var handler2 = registry.GetHandlerFor(new FakeCommand());
                    handler2.Handle(context);
                    handler2.Handle(context);
                }

                serviceProvider.Verify(mock => mock.GetService(typeof(FakeService)), Times.Once());
            }
        }

        public class WhenGettingHandlers
        {
            private readonly Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
            private readonly Mock<IStoreAggregates> aggregateStore = new Mock<IStoreAggregates>();
            private readonly Mock<ILocateTypes> typeLocator = new Mock<ILocateTypes>();

            [Fact]
            public void GetCommandHandlerBasedOnCommandType()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(ImplicitStrategyAggregate), typeof(AlternateImplicitStrategyAggregate) });

                var registry = new CommandHandlerRegistry(aggregateStore.Object, typeLocator.Object, serviceProvider.Object);
                var handler = registry.GetHandlerFor(new FakeCommand());

                Assert.Equal(typeof(ImplicitStrategyAggregate), handler.AggregateType);
            }

            [Fact]
            public void ThrowMappingExceptionIfHandlerNotFound()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(AlternateImplicitStrategyAggregate) });

                var registry = new CommandHandlerRegistry(aggregateStore.Object, typeLocator.Object, serviceProvider.Object);
                var ex = Assert.Throws<MappingException>(() => registry.GetHandlerFor(new FakeCommand()));

                Assert.Equal(Exceptions.CommandHandlerNotFound.FormatWith(typeof(FakeCommand)), ex.Message);
            }
        }

        [HandleByConvention, HandleByAttribute]
        internal class MultipleStrategiesAggregate : Aggregate
        { }

        internal class ImplicitStrategyAggregate : Aggregate
        {
            protected override bool RequiresExplicitCreate { get { return false; } }

            [UsedImplicitly]
            public void Handle(FakeCommand command)
            { }
        }

        internal class AlternateImplicitStrategyAggregate : Aggregate
        {
            protected override bool RequiresExplicitCreate { get { return false; } }

            [UsedImplicitly]
            public void Handle(AlternateFakeCommand command)
            { }
        }

        internal class ServicedAggregate : Aggregate
        {
            protected override bool RequiresExplicitCreate { get { return false; } }

            [UsedImplicitly]
            public void Handle(FakeCommand command, FakeService service)
            { }
        }

        [HandleByAttribute]
        internal class ExplicitStrategyAggregate : Aggregate
        {
            protected override bool RequiresExplicitCreate { get { return false; } }

            [UsedImplicitly, HandleMethod]
            public void Invoke(FakeCommand command)
            { }
        }

        internal class FakeCommand : Command
        { }

        [UsedImplicitly]
        internal class AlternateFakeCommand : Command
        { }

        internal class FakeService
        { }
    }
}
