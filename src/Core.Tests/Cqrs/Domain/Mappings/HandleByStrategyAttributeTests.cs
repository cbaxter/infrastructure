using System;
using System.Linq;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Domain.Mappings;
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
                var aggregate = new FakeAggregateWithDefaultServiceBehavior();
                var handleMethod = HandleByStrategyAttribute.Default.GetHandleMethods(typeof(FakeAggregateWithDefaultServiceBehavior), serviceProvider.Object).Single();

                handleMethod.Value(aggregate, new FakeCommand());
                handleMethod.Value(aggregate, new FakeCommand());

                serviceProvider.Verify(mock => mock.GetService(typeof(FakeService)), Times.Once());
            }

            [Fact]
            public void ResolveServiceAsSingletonIfMarkedWithAttribute()
            {
                var aggregate = new FakeAggregateWithSingletonServiceBehavior();
                var handleMethod = HandleByStrategyAttribute.Default.GetHandleMethods(typeof(FakeAggregateWithSingletonServiceBehavior), serviceProvider.Object).Single();

                handleMethod.Value(aggregate, new FakeCommand());
                handleMethod.Value(aggregate, new FakeCommand());

                serviceProvider.Verify(mock => mock.GetService(typeof(FakeService)), Times.Once());
            }

            [Fact]
            public void ResolveServiceAsTransientIfMarkedWithAttribute()
            {
                var aggregate = new FakeAggregateWithTransientServiceBehavior();
                var handleMethod = HandleByStrategyAttribute.Default.GetHandleMethods(typeof(FakeAggregateWithTransientServiceBehavior), serviceProvider.Object).Single();

                handleMethod.Value(aggregate, new FakeCommand());
                handleMethod.Value(aggregate, new FakeCommand());

                serviceProvider.Verify(mock => mock.GetService(typeof(FakeService)), Times.Exactly(2));
            }

            protected class FakeAggregateWithDefaultServiceBehavior : Aggregate
            {
                public void Handle(FakeCommand e, FakeService service)
                { }
            }

            protected class FakeAggregateWithSingletonServiceBehavior : Aggregate
            {
                public void Handle(FakeCommand e, [Singleton] FakeService service)
                { }
            }

            protected class FakeAggregateWithTransientServiceBehavior : Aggregate
            {
                public void Handle(FakeCommand e, [Transient] FakeService service)
                { }
            }

            protected class FakeCommand : Command
            { }

            protected class FakeService
            { }
        }
    }
}
