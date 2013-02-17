using System;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.Domain.Mappings;
using Spark.Infrastructure.Eventing;
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

namespace Spark.Infrastructure.Tests.Domain.Mappings
{
    public static class UsingApplyByAttributeAttribute
    {
        public class WhenLocatingApplyMethods
        {
            [Fact]
            public void DoNotUseConventionBasedMapping()
            {
                var attribute = new ApplyByAttributeAttribute();
                var applyMethods = attribute.GetApplyMethods(typeof(FakeAggregateWithNoAttribute));

                Assert.Equal(0, applyMethods.Count);
            }

            [Fact]
            public void MethodMustHaveVoidReturn()
            {
                var attribute = new ApplyByAttributeAttribute();
                var ex = Assert.Throws<Infrastructure.Domain.MappingException>(() => attribute.GetApplyMethods(typeof(FakeAggregateWithReturn)));

                Assert.Equal(Exceptions.AggregateApplyMethodMustHaveVoidReturn.FormatWith(typeof(FakeAggregateWithReturn), "OnFakeEvent"), ex.Message);
            }

            [Fact]
            public void MethodMustHaveSingleEventParameter()
            {
                var attribute = new ApplyByAttributeAttribute();
                var ex = Assert.Throws<Infrastructure.Domain.MappingException>(() => attribute.GetApplyMethods(typeof(FakeAggregateWithNoParameters)));

                Assert.Equal(Exceptions.AggregateApplyMethodInvalidParameters.FormatWith(typeof(Event), typeof(FakeAggregateWithNoParameters), "OnFakeEvent"), ex.Message);
            }

            [Fact]
            public void MethodNotMustHaveMultipleEventParameter()
            {
                var attribute = new ApplyByAttributeAttribute();
                var ex = Assert.Throws<Infrastructure.Domain.MappingException>(() => attribute.GetApplyMethods(typeof(FakeAggregateWithMultipleParameters)));

                Assert.Equal(Exceptions.AggregateApplyMethodInvalidParameters.FormatWith(typeof(Event), typeof(FakeAggregateWithMultipleParameters), "OnFakeEvent"), ex.Message);
            }

            protected class FakeAggregateWithNoAttribute : Aggregate
            {
                protected void Apply(FakeEvent e)
                { }
            }

            protected class FakeAggregateWithReturn : Aggregate
            {
                [ApplyMethod]
                protected Boolean OnFakeEvent(FakeEvent e)
                {
                    return false;
                }
            }

            protected class FakeAggregateWithNoParameters : Aggregate
            {
                [ApplyMethod]
                protected void OnFakeEvent()
                { }
            }

            protected class FakeAggregateWithMultipleParameters : Aggregate
            {
                [ApplyMethod]
                protected void OnFakeEvent(FakeEvent e1, FakeEvent e2)
                { }
            }

            protected class FakeEvent : Event
            { }
        }

        public class WhenPublicOnlySpecified
        {
            [Fact]
            public void MethodsNamedApplyAreIgnored()
            {
                var attribute = new ApplyByAttributeAttribute { PublicOnly = true };
                var applyMethods = attribute.GetApplyMethods(typeof(FakeAggregate));

                Assert.Equal(0, applyMethods.Count);
            }

            [Fact]
            public void MethodsMatchingCustomNameAreIncluded()
            {
                var attribute = new ApplyByAttributeAttribute { PublicOnly = false };
                var applyMethods = attribute.GetApplyMethods(typeof(FakeAggregate));

                Assert.Equal(1, applyMethods.Count);
            }

            protected class FakeAggregate : Aggregate
            {
                [ApplyMethod]
                protected void OnFakeEvent(FakeEvent e)
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
                var attribute = new ApplyByAttributeAttribute { ApplyOptional = applyOptional };
                var applyMethods = attribute.GetApplyMethods(typeof(FakeAggregate));

                Assert.Equal(applyOptional, applyMethods.ApplyOptional);
            }

            private sealed class FakeAggregate : Aggregate
            { }
        }
    }
}
