using System;
using System.Linq;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Domain.Mappings;
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

namespace Test.Spark.Cqrs.Domain.Mappings
{
    namespace UsingHandleByConventionAttribute
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
                var handleMethods = attribute.GetHandleMethods(typeof(FakeAggregate), new Mock<IServiceProvider>().Object);
                var handleMethod = handleMethods.Single().Value;
                var aggregate = new FakeAggregate();

                handleMethod(aggregate, new FakeCommand());

                Assert.True(aggregate.Handled);
            }

            [Fact]
            public void MethodsMatchingCustomNameAreIncluded()
            {
                var attribute = new HandleByConventionAttribute { MethodName = "Custom" };
                var handleMethods = attribute.GetHandleMethods(typeof(FakeAggregate), new Mock<IServiceProvider>().Object);
                var handleMethod = handleMethods.Single().Value;
                var aggregate = new FakeAggregate();

                handleMethod(aggregate, new FakeCommand());

                Assert.True(aggregate.Handled);
            }

            protected class FakeAggregate : Aggregate
            {
                public Boolean Handled { get; private set; }
                public void Handle(FakeCommand e)
                {
                    throw new MethodAccessException();
                }

                public void Custom(FakeCommand e)
                {
                    Handled = true;
                }
            }

            protected class FakeCommand : Command
            { }
        }

        public class WhenPublicOnlySpecified
        {
            [Fact]
            public void PublicMethodsAreIncluded()
            {
                var attribute = new HandleByConventionAttribute { PublicOnly = false };
                var handleMethods = attribute.GetHandleMethods(typeof(FakeAggregate), new Mock<IServiceProvider>().Object);

                Assert.Equal(1, handleMethods.Count);
            }

            [Fact]
            public void NonPublicMethodsAreExcluded()
            {
                var attribute = new HandleByConventionAttribute { PublicOnly = true };
                var handleMethods = attribute.GetHandleMethods(typeof(FakeAggregate), new Mock<IServiceProvider>().Object);

                Assert.Equal(0, handleMethods.Count);
            }

            protected class FakeAggregate : Aggregate
            {
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
                var attribute = new HandleByConventionAttribute { PublicOnly = false };
                var expectedException = new MappingException(Exceptions.HandleMethodOverloaded.FormatWith(typeof(FakeAggregate), "Void Handle(FakeCommand, FakeService, FakeService)"));

                serviceProvider.Setup(mock => mock.GetService(typeof(FakeService))).Returns(null);

                var actualException = Assert.Throws<MappingException>(() => attribute.GetHandleMethods(typeof(FakeAggregate), serviceProvider.Object));

                Assert.Equal(expectedException.Message, actualException.Message);
            }

            protected class FakeAggregate : Aggregate
            {
                public void Handle(FakeCommand e, FakeService service)
                { }

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
