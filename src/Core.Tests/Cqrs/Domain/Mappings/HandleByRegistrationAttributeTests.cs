using System;
using System.Reflection;
using Moq;
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
    public static class UsingHandleByRegistrationAttribute
    {
        public class WhenLocatingHandleMethods
        {
            [Fact]
            public void CanExplicitlyMapPrivateMemberWithoutReflection()
            {
                var serviceProvider = new Mock<IServiceProvider>();
                var attribute = typeof(FakeAggregate).GetCustomAttribute<HandleByRegistrationAttribute>();

                serviceProvider.Setup(mock => mock.GetService(typeof(FakeService))).Returns(new FakeService());

                var handleMethods = attribute.GetHandleMethods(typeof(FakeAggregate), serviceProvider.Object);

                Assert.Equal(1, handleMethods.Count);
            }

            [HandleByRegistration(typeof(FakeHandleMethodMapping))]
            protected class FakeAggregate : Aggregate
            {
                private class FakeHandleMethodMapping : HandleMethodMapping
                {
                    protected override void RegisterMappings(HandleMethodMappingBuilder builder)
                    {
                        var service = builder.GetService<FakeService>();

                        builder.Register(typeof(FakeCommand), (aggregate, e) => ((FakeAggregate)aggregate).OnFakeEvent((FakeCommand)e, service));
                    }
                }

                private void OnFakeEvent(FakeCommand e, FakeService service)
                { }
            }

            protected class FakeCommand : Command
            { }

            protected class FakeService
            { }
        }
    }
}
