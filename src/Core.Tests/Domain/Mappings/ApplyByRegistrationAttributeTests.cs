using System;
using System.Reflection;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.Domain.Mappings;
using Spark.Infrastructure.Eventing;
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

namespace Spark.Infrastructure.Tests.Domain.Mappings
{
    public static class UsingApplyByRegistrationAttribute
    {
        public class WhenLocatingApplyMethods
        {
            [Fact]
            public void CanExplicitlyMapPrivateMemberWithoutReflection()
            {
                var attribute = typeof(FakeAggregate).GetCustomAttribute<ApplyByRegistrationAttribute>();
                var applyMethods = attribute.GetApplyMethods(typeof(FakeAggregate));

                Assert.Equal(1, applyMethods.Count);
            }

            [ApplyByRegistration(typeof(FakeApplyMethodMapping))]
            protected class FakeAggregate : Aggregate
            {
                private class FakeApplyMethodMapping : ApplyMethodMapping
                {
                    protected override void RegisterMappings(ApplyMethodMappingBuilder builder)
                    {
                        builder.Register(typeof(FakeEvent), (aggregate, e) => ((FakeAggregate)aggregate).OnFakeEvent((FakeEvent)e));
                    }
                }

                private void OnFakeEvent(FakeEvent e)
                { }
            }

            protected class FakeEvent : Event
            { }
        }

        public class WhenApplyOptionalSpecified
        {
            [Theory, InlineData(true), InlineData(false)]
            public void PropagateSettingToApplyMethodCollection(Boolean applyOptional)
            {
                var attribute = new ApplyByRegistrationAttribute(typeof(FakeMapping)) { ApplyOptional = applyOptional };
                var applyMethods = attribute.GetApplyMethods(typeof(FakeAggregate));

                Assert.Equal(applyOptional, applyMethods.ApplyOptional);
            }

            public sealed class FakeMapping : ApplyMethodMapping
            {
                protected override void RegisterMappings(ApplyMethodMappingBuilder builder)
                { }
            }

            private sealed class FakeAggregate : Aggregate
            { }
        }
    }
}
