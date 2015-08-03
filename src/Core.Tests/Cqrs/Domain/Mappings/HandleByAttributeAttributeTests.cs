using System;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Domain.Mappings;
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

namespace Test.Spark.Cqrs.Domain.Mappings
{
    namespace UsingHandleByAttributeAttribute
    {
        public class WhenLocatingHandleMethods
        {
            [Fact]
            public void DoNotUseConventionBasedMapping()
            {
                var attribute = new HandleByAttributeAttribute();
                var handleMethods = attribute.GetHandleMethods(typeof(FakeAggregateWithNoAttribute), new Mock<IServiceProvider>().Object);

                Assert.Equal(0, handleMethods.Count);
            }

            [Fact]
            public void MethodMustHaveVoidReturn()
            {
                var attribute = new HandleByAttributeAttribute();
                var ex = Assert.Throws<MappingException>(() => attribute.GetHandleMethods(typeof(FakeAggregateWithReturn), new Mock<IServiceProvider>().Object));

                Assert.Equal(Exceptions.HandleMethodMustHaveVoidReturn.FormatWith(typeof(FakeAggregateWithReturn), "OnFakeCommand"), ex.Message);
            }

            [Fact]
            public void MethodMustHaveCommandAsFirstParameter()
            {
                var attribute = new HandleByAttributeAttribute();
                var ex = Assert.Throws<MappingException>(() => attribute.GetHandleMethods(typeof(FakeAggregateWithNoParameters), new Mock<IServiceProvider>().Object));

                Assert.Equal(Exceptions.HandleMethodInvalidParameters.FormatWith(typeof(Command), typeof(FakeAggregateWithNoParameters), "OnFakeCommand"), ex.Message);
            }

            protected class FakeAggregateWithNoAttribute : Aggregate
            {
                protected void Handle(FakeCommand e)
                { }
            }

            protected class FakeAggregateWithReturn : Aggregate
            {
                [HandleMethod]
                protected Boolean OnFakeCommand(FakeCommand e)
                {
                    return false;
                }
            }

            protected class FakeAggregateWithNoParameters : Aggregate
            {
                [HandleMethod]
                protected void OnFakeCommand()
                { }
            }

            protected class FakeAggregateWithMultipleParameters : Aggregate
            {
                [HandleMethod]
                protected void OnFakeCommand(FakeCommand e1, FakeCommand e2)
                { }
            }

            protected class FakeCommand : Command
            { }
        }

        public class WhenPublicOnlySpecified
        {
            [Fact]
            public void PublicMethodsAreIncluded()
            {
                var attribute = new HandleByAttributeAttribute { PublicOnly = false };
                var handleMethods = attribute.GetHandleMethods(typeof(FakeAggregate), new Mock<IServiceProvider>().Object);

                Assert.Equal(1, handleMethods.Count);
            }

            [Fact]
            public void NonPublicMethodsAreExcluded()
            {
                var attribute = new HandleByAttributeAttribute { PublicOnly = true };
                var handleMethods = attribute.GetHandleMethods(typeof(FakeAggregate), new Mock<IServiceProvider>().Object);

                Assert.Equal(0, handleMethods.Count);
            }

            protected class FakeAggregate : Aggregate
            {
                [HandleMethod]
                protected void Handle(FakeCommand e)
                { }
            }

            protected class FakeCommand : Command
            { }
        }

        public class WhenOneOrMoreServicesRequired
        {
            [Fact]
            public void CannotOverloadHandleMethods()
            {
                var serviceProvider = new Mock<IServiceProvider>();
                var attribute = new HandleByAttributeAttribute { PublicOnly = false };
                var expectedException = new MappingException(Exceptions.HandleMethodOverloaded.FormatWith(typeof(FakeAggregate), "Void Handle(FakeCommand, FakeService, FakeService)"));

                serviceProvider.Setup(mock => mock.GetService(typeof(FakeService))).Returns(null);

                var actualException = Assert.Throws<MappingException>(() => attribute.GetHandleMethods(typeof(FakeAggregate), serviceProvider.Object));

                Assert.Equal(expectedException.Message, actualException.Message);
            }

            protected class FakeAggregate : Aggregate
            {
                [HandleMethod]
                public void Handle(FakeCommand e, FakeService service)
                { }

                [HandleMethod]
                protected void Handle(FakeCommand e, FakeService service1, FakeService service2)
                { }
            }

            protected class FakeCommand : Command
            { }

            protected class FakeService
            { }
        }
    }
}
