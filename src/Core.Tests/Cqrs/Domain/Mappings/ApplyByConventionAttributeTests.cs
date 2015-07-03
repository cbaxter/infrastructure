using System;
using System.Linq;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Domain.Mappings;
using Spark.Cqrs.Eventing;
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
    namespace UsingApplyByConventionAttribute
    {
        public class WhenUsingDefaultApplyMethodMappingAttribute
        {
            [Fact]
            public void DefaultIsApplyByConventionAttribute()
            {
                Assert.IsType(typeof(ApplyByConventionAttribute), ApplyByStrategyAttribute.Default);
            }
        }

        public class WhenCustomMethodNameSpecified
        {
            [Fact]
            public void MethodsNamedApplyAreIgnored()
            {
                var attribute = new ApplyByConventionAttribute { MethodName = "Custom" };
                var applyMethods = attribute.GetApplyMethods(typeof(FakeAggregate));
                var applyMethod = applyMethods.Single().Value;
                var aggregate = new FakeAggregate();

                applyMethod(aggregate, new FakeEvent());

                Assert.True(aggregate.Handled);
            }

            [Fact]
            public void MethodsMatchingCustomNameAreIncluded()
            {
                var attribute = new ApplyByConventionAttribute { MethodName = "Custom" };
                var applyMethods = attribute.GetApplyMethods(typeof(FakeAggregate));
                var applyMethod = applyMethods.Single().Value;
                var aggregate = new FakeAggregate();

                applyMethod(aggregate, new FakeEvent());

                Assert.True(aggregate.Handled);
            }

            protected class FakeAggregate : Aggregate
            {
                public Boolean Handled { get; private set; }

                public void Apply(FakeEvent e)
                {
                    throw new MethodAccessException();
                }

                public void Custom(FakeEvent e)
                {
                    Handled = true;
                }
            }

            protected class FakeEvent : Event
            { }
        }


        public class WhenPublicOnlySpecified
        {
            [Fact]
            public void MethodsNamedApplyAreIgnored()
            {
                var attribute = new ApplyByConventionAttribute { PublicOnly = true };
                var applyMethods = attribute.GetApplyMethods(typeof(FakeAggregate));

                Assert.Equal(0, applyMethods.Count);
            }

            [Fact]
            public void MethodsMatchingCustomNameAreIncluded()
            {
                var attribute = new ApplyByConventionAttribute { PublicOnly = false };
                var applyMethods = attribute.GetApplyMethods(typeof(FakeAggregate));

                Assert.Equal(1, applyMethods.Count);
            }

            protected class FakeAggregate : Aggregate
            {
                protected void Apply(FakeEvent e)
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
                var attribute = new ApplyByConventionAttribute { ApplyOptional = applyOptional };
                var applyMethods = attribute.GetApplyMethods(typeof(FakeAggregate));

                Assert.Equal(applyOptional, applyMethods.ApplyOptional);
            }

            private sealed class FakeAggregate : Aggregate
            { }
        }
    }
}
