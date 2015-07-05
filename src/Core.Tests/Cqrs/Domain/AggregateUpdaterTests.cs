using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Moq;
using Spark;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Domain.Mappings;
using Spark.Cqrs.Eventing;
using Spark.Resources;
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

namespace Test.Spark.Cqrs.Domain
{
    namespace UsingAggregateUpdater
    {
        public class WhenCreatingNewUpdater
        {
            private readonly Mock<ILocateTypes> typeLocator = new Mock<ILocateTypes>();

            [Fact]
            public void NoAggregatesRequired()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new Type[0]);

                Assert.NotNull(new AggregateUpdater(typeLocator.Object));
            }

            [Fact]
            public void ThrowMappingExceptionIfMoreThanOneMappingStrategyDefined()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(MultipleStrategiesAggregate) });

                var ex = Assert.Throws<MappingException>(() => new AggregateUpdater(typeLocator.Object));

                Assert.Equal(Exceptions.AggregateAmbiguousApplyMethodStrategy.FormatWith(typeof(MultipleStrategiesAggregate)), ex.Message);
            }

            [Fact]
            public void ThrowMappingExceptionIfNoPublicDefaultConstructor()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(NoDefaultConstructorAggregate) });

                var ex = Assert.Throws<MappingException>(() => new AggregateUpdater(typeLocator.Object));

                Assert.Equal(Exceptions.AggregateDefaultConstructorRequired.FormatWith(typeof(NoDefaultConstructorAggregate)), ex.Message);
            }
        }

        public class WhenApplyingEvent
        {
            private readonly Mock<ILocateTypes> typeLocator = new Mock<ILocateTypes>();

            [Fact]
            public void AggregateMustHaveAtLeastOneKnownApplyMethod()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new Type[0]);

                var updater = new AggregateUpdater(typeLocator.Object);
                var aggregate = new ImplicitStrategyAggregate();
                var e = new FakeEvent();

                var ex = Assert.Throws<MappingException>(() => updater.Apply(e, aggregate));

                Assert.Equal(Exceptions.AggregateTypeUndiscovered.FormatWith(typeof(ImplicitStrategyAggregate)), ex.Message);
            }

            [Fact]
            public void DefaultMappingStrategyUsedWhenNoExplicitStrategyDefined()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(ImplicitStrategyAggregate) });

                var updater = new AggregateUpdater(typeLocator.Object);
                var aggregate = new ImplicitStrategyAggregate();
                var e = new FakeEvent();

                updater.Apply(e, aggregate);

                Assert.Same(e, aggregate.AppliedEvents.Single());
            }

            [Fact]
            public void CustomMappingStrategyUsedWhenExplicitStrategyDefined()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(ExplicitStrategyAggregate) });

                var updater = new AggregateUpdater(typeLocator.Object);
                var aggregate = new ExplicitStrategyAggregate();
                var e = new FakeEvent();

                updater.Apply(e, aggregate);

                Assert.Same(e, aggregate.AppliedEvents.Single());
            }

            [Fact]
            public void CanApplyEventIfApplyMethodOptionalAndNotDefined()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(OptionalApplyAggregate) });

                var updater = new AggregateUpdater(typeLocator.Object);
                var aggregate = new OptionalApplyAggregate();
                var e = new FakeEvent();

                updater.Apply(e, aggregate);
            }

            [Fact]
            public void ThrowMappingExceptionIfApplyMethodRequiredAndNotDefined()
            {
                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(RequiredApplyAggregate) });

                var e = new FakeEvent();
                var aggregate = new RequiredApplyAggregate();
                var updater = new AggregateUpdater(typeLocator.Object);
                var ex = Assert.Throws<MappingException>(() => updater.Apply(e, aggregate));

                Assert.Equal(Exceptions.AggregateApplyMethodNotFound.FormatWith(typeof(RequiredApplyAggregate), typeof(FakeEvent)), ex.Message);
            }
        }

        internal class NoDefaultConstructorAggregate : Aggregate
        {
            public NoDefaultConstructorAggregate(Object arg) { }
        }

        [ApplyByConvention, ApplyByAttribute]
        internal class MultipleStrategiesAggregate : Aggregate
        { }

        internal class ImplicitStrategyAggregate : Aggregate
        {
            public readonly IList<Event> AppliedEvents = new List<Event>();

            [UsedImplicitly]
            protected void Apply(FakeEvent e)
            {
                AppliedEvents.Add(e);
            }
        }

        [ApplyByAttribute]
        internal class ExplicitStrategyAggregate : Aggregate
        {
            public readonly IList<Event> AppliedEvents = new List<Event>();

            [UsedImplicitly, ApplyMethod]
            protected void Update(FakeEvent e)
            {
                AppliedEvents.Add(e);
            }
        }

        [ApplyByConvention(ApplyOptional = true)]
        internal class OptionalApplyAggregate : Aggregate
        { }

        [ApplyByConvention(ApplyOptional = false)]
        internal class RequiredApplyAggregate : Aggregate
        { }

        internal class FakeEvent : Event
        { }
    }
}
