using System;
using System.Linq;
using Moq;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Eventing.Mappings;
using Spark.Infrastructure.Resources;
using Xunit;
using Xunit.Extensions;

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

namespace Spark.Infrastructure.Tests.Eventing.Mappings
{
    public static class UsingHandleByConventionAttribute
    {
        public class WhenUsingDefaultHandleMethodMappingAttribute
        {
            [Fact]
            public void DefaultIsHandleByConventionAttribute()
            {
                Assert.IsType(typeof(HandleByConventionAttribute), HandleByStrategyAttribute.Default);
            }
        }

        public class WhenCustomMethodNameSpecified
        {
            [Fact]
            public void MethodsNamedHandleAreIgnored()
            {
                var attribute = new HandleByConventionAttribute { MethodName = "Custom" };
                var handleMethods = attribute.GetHandleMethods(typeof(FakeHandler), new Mock<IServiceProvider>().Object);
                var handleMethod = handleMethods.Single().Value;

                Assert.DoesNotThrow(() => handleMethod(new FakeHandler(), new FakeEvent()));
            }

            [Fact]
            public void MethodsMatchingCustomNameAreIncluded()
            {
                var attribute = new HandleByConventionAttribute { MethodName = "Custom" };
                var handleMethods = attribute.GetHandleMethods(typeof(FakeHandler), new Mock<IServiceProvider>().Object);
                var handleMethod = handleMethods.Single().Value;

                Assert.DoesNotThrow(() => handleMethod(new FakeHandler(), new FakeEvent()));
            }

            [EventHandler]
            protected class FakeHandler
            {
                public void Handle(FakeEvent e)
                {
                    throw new MethodAccessException();
                }

                public void Custom(FakeEvent e)
                { }
            }

            protected class FakeEvent : Event
            { }
        }

        public class WhenPublicOnlySpecified
        {
            [Fact]
            public void PublicMethodsAreIncluded()
            {
                var attribute = new HandleByConventionAttribute { PublicOnly = false };
                var handleMethods = attribute.GetHandleMethods(typeof(FakeHandler), new Mock<IServiceProvider>().Object);

                Assert.Equal(1, handleMethods.Count);
            }

            [Fact]
            public void NonPublicMethodsAreExcluded()
            {
                var attribute = new HandleByConventionAttribute { PublicOnly = true };
                var handleMethods = attribute.GetHandleMethods(typeof(FakeHandler), new Mock<IServiceProvider>().Object);

                Assert.Equal(0, handleMethods.Count);
            }

            [EventHandler]
            protected class FakeHandler
            {
                protected void Handle(FakeEvent e)
                { }
            }

            protected class FakeEvent : Event
            { }
        }

        public class WhenOneOrMoreServicesRequired
        {
            [Fact]
            public void CannotOverloadHandleMethods()
            {
                var serviceProvider = new Mock<IServiceProvider>();
                var attribute = new HandleByConventionAttribute { PublicOnly = false };
                var expectedException = new MappingException(Exceptions.HandleMethodOverloaded.FormatWith(typeof(FakeHandler), "Void Handle(FakeEvent, FakeService, FakeService)"));

                serviceProvider.Setup(mock => mock.GetService(typeof(FakeService))).Returns(null);

                var actualException = Assert.Throws<MappingException>(() => attribute.GetHandleMethods(typeof(FakeHandler), serviceProvider.Object));

                Assert.Equal(expectedException.Message, actualException.Message);
            }

            [EventHandler]
            protected class FakeHandler
            {
                public void Handle(FakeEvent e, FakeService service)
                { }

                protected void Handle(FakeEvent e, FakeService service1, FakeService service2)
                { }
            }

            protected class FakeEvent : Event
            { }

            protected class FakeService
            { }
        }
    }
}
