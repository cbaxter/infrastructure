using System;
using System.Reflection;
using Moq;
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
    namespace UsingHandleByRegistrationAttribute
    {
        public class WhenLocatingHandleMethods
        {
            [Fact]
            public void CanExplicitlyMapPrivateMemberWithoutReflection()
            {
                var serviceProvider = new Mock<IServiceProvider>();
                var attribute = typeof(FakeHandler).GetCustomAttribute<HandleByRegistrationAttribute>();

                serviceProvider.Setup(mock => mock.GetService(typeof (FakeService))).Returns(new FakeService());

                var handleMethods = attribute.GetHandleMethods(typeof(FakeHandler), serviceProvider.Object);

                Assert.Equal(1, handleMethods.Count);
            }

            [HandleByRegistration(typeof(FakeHandleMethodMapping))]
            protected class FakeHandler
            {
                private class FakeHandleMethodMapping : HandleMethodMapping
                {
                    protected override void RegisterMappings(HandleMethodMappingBuilder builder)
                    {
                        var service = builder.GetService<FakeService>();

                        builder.Register(typeof(FakeEvent), (aggregate, e) => ((FakeHandler)aggregate).OnFakeEvent((FakeEvent)e, service));
                    }
                }

                private void OnFakeEvent(FakeEvent e, FakeService service)
                { }
            }

            protected class FakeEvent : Event
            { }

            protected class FakeService
            { }
        }
    }
}
