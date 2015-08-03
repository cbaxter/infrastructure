using System;
using Spark;
using Spark.Cqrs.Domain.Mappings;
using Spark.Cqrs.Eventing;
using Spark.Resources;
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
    namespace UsingApplyMethodMapping
    {
        public class WhenCreatingMapping
        {
            [Fact]
            public void EventTypeMustDeriveFromEvent()
            {
                // ReSharper disable NotResolvedInText
                var mapping = new ObjectEventTypeMapping();
                var expectedEx = new ArgumentException(Exceptions.TypeDoesNotDeriveFromBase.FormatWith(typeof (Event), typeof (Object)), "eventType");
                var ex = Assert.Throws<ArgumentException>(() => mapping.GetMappings());

                Assert.Equal(expectedEx.Message, ex.Message);
                // ReSharper restore NotResolvedInText
            }

            protected class ObjectEventTypeMapping : ApplyMethodMapping
            {
                protected override void RegisterMappings(ApplyMethodMappingBuilder builder)
                {
                    builder.Register(typeof(Object), (aggregate, @event) => { throw new Exception(); });
                }
            }
        }
    }
}
