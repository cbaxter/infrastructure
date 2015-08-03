using System;
using System.Reflection;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Domain.Mappings;
using Spark.Cqrs.Eventing;
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
    namespace UsingApplyByRegistrationAttribute
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
