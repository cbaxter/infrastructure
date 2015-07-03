using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Mappings;
using Spark.Cqrs.Eventing.Sagas;
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

namespace Test.Spark.Cqrs.Eventing
{
    namespace UsingEventHandlerRegistry
    {
        public class WhenCreatingNewRegistry
        {
            private readonly Mock<IPublishCommands> commandPublisher = new Mock<IPublishCommands>();
            private readonly Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
            private readonly Mock<IStoreSagas> sagaStore = new Mock<IStoreSagas>();

            [Fact]
            public void NoHandlersRequired()
            {
                var typeLocator = new FakeTypeLocator();

                Assert.NotNull(new EventHandlerRegistry(typeLocator, serviceProvider.Object, sagaStore.Object, new Lazy<IPublishCommands>(() => commandPublisher.Object)));
            }

            [Fact]
            public void ThrowMappingExceptionIfMoreThanOneMappingStrategyDefined()
            {
                var typeLocator = new FakeTypeLocator(typeof(FakeEvent), typeof(MultipleStrategiesHandler));
                var ex = Assert.Throws<MappingException>(() => new EventHandlerRegistry(typeLocator, serviceProvider.Object, sagaStore.Object, new Lazy<IPublishCommands>(() => commandPublisher.Object)));

                Assert.Equal(Exceptions.EventHandlerHandleByStrategyAmbiguous.FormatWith(typeof(MultipleStrategiesHandler)), ex.Message);
            }

            [Fact]
            public void ThrowMappingExceptionIfSagaDoesNotHaveDefaultPublicConstructor()
            {
                var typeLocator = new FakeTypeLocator(typeof(FakeEvent), typeof(FakeSaga));
                var ex = Assert.Throws<MappingException>(() => new EventHandlerRegistry(typeLocator, serviceProvider.Object, sagaStore.Object, new Lazy<IPublishCommands>(() => commandPublisher.Object)));

                Assert.Equal(Exceptions.SagaDefaultConstructorRequired.FormatWith(typeof(FakeSaga)), ex.Message);
            }

            [Fact]
            public void ThrowMappingExceptionIfNotAllHandleMethodsConfigured()
            {
                var typeLocator = new FakeTypeLocator(typeof(FakeEvent), typeof(FakeSagaWithMissingConfiguration));
                var ex = Assert.Throws<MappingException>(() => new EventHandlerRegistry(typeLocator, serviceProvider.Object, sagaStore.Object, new Lazy<IPublishCommands>(() => commandPublisher.Object)));

                Assert.Equal(Exceptions.EventTypeNotConfigured.FormatWith(typeof(FakeSagaWithMissingConfiguration), typeof(FakeEvent)), ex.Message);
            }

            [Fact]
            public void ThrowMappingExceptionIfSagaDoesNotHaveAtLeastOneInitiatingEvent()
            {
                var typeLocator = new FakeTypeLocator(typeof(FakeEvent), typeof(FakeSagaWithNoStartingEvent));
                var ex = Assert.Throws<MappingException>(() => new EventHandlerRegistry(typeLocator, serviceProvider.Object, sagaStore.Object, new Lazy<IPublishCommands>(() => commandPublisher.Object)));

                Assert.Equal(Exceptions.SagaMustHaveAtLeastOneInitiatingEvent.FormatWith(typeof(FakeSagaWithNoStartingEvent)), ex.Message);
            }

            [Fact]
            public void DefaultMappingStrategyUsedWhenNoExplicitStrategyDefined()
            {
                var typeLocator = new FakeTypeLocator(typeof(FakeEvent), typeof(ImplicitStrategyAggregate));
                var registry = new EventHandlerRegistry(typeLocator, serviceProvider.Object, sagaStore.Object, new Lazy<IPublishCommands>(() => commandPublisher.Object));
                var handler = registry.GetHandlersFor(new FakeEvent());

                Assert.Equal(1, handler.Count());
            }

            [Fact]
            public void CustomMappingStrategyUsedWhenExplicitStrategyDefined()
            {
                var typeLocator = new FakeTypeLocator(typeof(FakeEvent), typeof(ExplicitStrategyAggregate));
                var registry = new EventHandlerRegistry(typeLocator, serviceProvider.Object, sagaStore.Object, new Lazy<IPublishCommands>(() => commandPublisher.Object));
                var handler = registry.GetHandlersFor(new FakeEvent());

                Assert.Equal(1, handler.Count());
            }

            [EventHandler, HandleByConvention, HandleByAttribute]
            private class MultipleStrategiesHandler
            { }

            [EventHandler, HandleByAttribute]
            private class ExplicitStrategyAggregate : Aggregate
            {
                [UsedImplicitly, HandleMethod]
                public void Handle(FakeEvent e)
                { }
            }

            [EventHandler]
            private class ImplicitStrategyAggregate : Aggregate
            {
                [UsedImplicitly]
                public void Handle(FakeEvent e)
                { }
            }

            private sealed class FakeSaga : Saga
            {
                public FakeSaga([UsedImplicitly] Object dependency)
                { }

                protected override void Configure(SagaConfiguration saga)
                { }
            }

            private sealed class FakeSagaWithNoStartingEvent : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanHandle((FakeEvent e) => e.Id);
                }

                [UsedImplicitly]
                public void Handle(FakeEvent e)
                { }
            }

            private sealed class FakeSagaWithMissingConfiguration : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                { }

                [UsedImplicitly]
                public void Handle(FakeEvent e)
                { }
            }

            public sealed class FakeEvent : Event
            {
                public Guid Id { get; set; }
            }
        }

        public class WhenGettingHandlers
        {
            private readonly Mock<IPublishCommands> commandPublisher = new Mock<IPublishCommands>();
            private readonly Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
            private readonly Mock<IStoreSagas> sagaStore = new Mock<IStoreSagas>();

            [Fact]
            public void ReturnBaseTypesBeforeDerivedTypes()
            {
                var typeLocator = new FakeTypeLocator(typeof(PrimaryHandler), typeof(FakeEvent), typeof(DerivedFakeEvent));
                var registry = new EventHandlerRegistry(typeLocator, serviceProvider.Object, sagaStore.Object, new Lazy<IPublishCommands>(() => commandPublisher.Object));
                var handlers = registry.GetHandlersFor(new DerivedFakeEvent()).ToArray();

                Assert.Equal(typeof(FakeEvent), handlers[0].EventType);
                Assert.Equal(typeof(DerivedFakeEvent), handlers[1].EventType);
            }

            [Fact]
            public void ReturnEventHandlersBeforeSagaEventHandlers()
            {
                var typeLocator = new FakeTypeLocator(typeof(PrimaryHandler), typeof(PrimarySaga), typeof(FakeEvent));
                var registry = new EventHandlerRegistry(typeLocator, serviceProvider.Object, sagaStore.Object, new Lazy<IPublishCommands>(() => commandPublisher.Object));
                var handlers = registry.GetHandlersFor(new FakeEvent()).ToArray();

                Assert.Equal(typeof(PrimaryHandler), handlers[0].HandlerType);
                Assert.Equal(typeof(PrimarySaga), handlers[1].HandlerType);
            }

            [Fact]
            public void ReturnHandlersOfSameEventTypeSortedByHandlerFullName()
            {
                var typeLocator = new FakeTypeLocator(typeof(PrimaryHandler), typeof(SecondaryHandler), typeof(PrimarySaga), typeof(SecondarySaga), typeof(FakeEvent));
                var registry = new EventHandlerRegistry(typeLocator, serviceProvider.Object, sagaStore.Object, new Lazy<IPublishCommands>(() => commandPublisher.Object));
                var handlers = registry.GetHandlersFor(new FakeEvent()).ToArray();

                Assert.Equal(typeof(PrimaryHandler), handlers[0].HandlerType);
                Assert.Equal(typeof(SecondaryHandler), handlers[1].HandlerType);
                Assert.Equal(typeof(PrimarySaga), handlers[2].HandlerType);
                Assert.Equal(typeof(SecondarySaga), handlers[3].HandlerType);
            }

            [EventHandler]
            private class PrimaryHandler
            {
                [UsedImplicitly]
                public void Handle(FakeEvent e)
                { }

                [UsedImplicitly]
                public void Handle(AlternateFakeEvent e)
                { }

                [UsedImplicitly]
                public void Handle(DerivedFakeEvent e)
                { }
            }

            [EventHandler]
            private class SecondaryHandler
            {
                [UsedImplicitly]
                public void Handle(FakeEvent e)
                { }

                [UsedImplicitly]
                public void Handle(AlternateFakeEvent e)
                { }

                [UsedImplicitly]
                public void Handle(DerivedFakeEvent e)
                { }
            }

            private class PrimarySaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanStartWith((FakeEvent e) => Guid.Empty);
                    saga.CanStartWith((AlternateFakeEvent e) => Guid.Empty);
                    saga.CanStartWith((DerivedFakeEvent e) => Guid.Empty);
                    saga.CanHandle((Timeout e) => Guid.Empty);
                }

                [UsedImplicitly]
                public void Handle(Timeout e)
                { }

                [UsedImplicitly]
                public void Handle(FakeEvent e)
                { }

                [UsedImplicitly]
                public void Handle(AlternateFakeEvent e)
                { }

                [UsedImplicitly]
                public void Handle(DerivedFakeEvent e)
                { }
            }

            private class SecondarySaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanStartWith((FakeEvent e) => Guid.Empty);
                    saga.CanStartWith((AlternateFakeEvent e) => Guid.Empty);
                    saga.CanStartWith((DerivedFakeEvent e) => Guid.Empty);
                    saga.CanHandle((Timeout e) => Guid.Empty);
                }

                [UsedImplicitly]
                public void Handle(Timeout e)
                { }

                [UsedImplicitly]
                public void Handle(FakeEvent e)
                { }

                [UsedImplicitly]
                public void Handle(AlternateFakeEvent e)
                { }

                [UsedImplicitly]
                public void Handle(DerivedFakeEvent e)
                { }
            }

            [UsedImplicitly]
            private class FakeEvent : Event
            { }

            [UsedImplicitly]
            private class AlternateFakeEvent : Event
            { }

            [UsedImplicitly]
            private class DerivedFakeEvent : FakeEvent
            { }
        }

        internal class FakeTypeLocator : ILocateTypes
        {
            private readonly IEnumerable<Type> knownTypes;

            public FakeTypeLocator(params Type[] knownTypes)
            {
                this.knownTypes = knownTypes;
            }

            public Type[] GetTypes(Func<Type, Boolean> predicate)
            {
                return knownTypes.Where(predicate).ToArray();
            }
        }
    }
}
