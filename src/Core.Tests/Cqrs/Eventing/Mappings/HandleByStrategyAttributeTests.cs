using System;
using System.Linq;
using Moq;
using Spark;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Mappings;
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

namespace Test.Spark.Cqrs.Eventing.Mappings
{
    namespace UsingHandleByStrategyAttribute
    {
        public class WhenResolvingServices
        {
            private readonly Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();

            public WhenResolvingServices()
            {
                serviceProvider.Setup(mock => mock.GetService(typeof(FakeService))).Returns(new FakeService());
            }

            [Fact]
            public void ResolveServiceAsSingletonByDefault()
            {
                var handler = new FakeHandlerWithDefaultServiceBehavior();
                var handleMethod = HandleByStrategyAttribute.Default.GetHandleMethods(typeof(FakeHandlerWithDefaultServiceBehavior), serviceProvider.Object).Single();

                handleMethod.Value(handler, new FakeEvent());
                handleMethod.Value(handler, new FakeEvent());

                serviceProvider.Verify(mock => mock.GetService(typeof(FakeService)), Times.Once());
            }

            [Fact]
            public void ResolveServiceAsSingletonIfMarkedWithAttribute()
            {
                var handler = new FakeHandlerWithSingletonServiceBehavior();
                var handleMethod = HandleByStrategyAttribute.Default.GetHandleMethods(typeof(FakeHandlerWithSingletonServiceBehavior), serviceProvider.Object).Single();

                handleMethod.Value(handler, new FakeEvent());
                handleMethod.Value(handler, new FakeEvent());

                serviceProvider.Verify(mock => mock.GetService(typeof(FakeService)), Times.Once());
            }

            [Fact]
            public void ResolveServiceAsTransientIfMarkedWithAttribute()
            {
                var handler = new FakeHandlerWithTransientServiceBehavior();
                var handleMethod = HandleByStrategyAttribute.Default.GetHandleMethods(typeof(FakeHandlerWithTransientServiceBehavior), serviceProvider.Object).Single();

                handleMethod.Value(handler, new FakeEvent());
                handleMethod.Value(handler, new FakeEvent());

                serviceProvider.Verify(mock => mock.GetService(typeof(FakeService)), Times.Exactly(2));
            }

            protected class FakeHandlerWithDefaultServiceBehavior
            {
                public void Handle(FakeEvent e, FakeService service)
                { }
            }

            protected class FakeHandlerWithSingletonServiceBehavior
            {
                public void Handle(FakeEvent e, [Singleton] FakeService service)
                { }
            }

            protected class FakeHandlerWithTransientServiceBehavior
            {
                public void Handle(FakeEvent e, [Transient] FakeService service)
                { }
            }

            protected class FakeEvent : Event
            { }

            protected class FakeService
            { }
        }
    }
}
