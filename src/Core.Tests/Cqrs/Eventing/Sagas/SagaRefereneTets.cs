using System;
using Spark;
using Spark.Cqrs.Eventing.Sagas;
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

namespace Test.Spark.Cqrs.Eventing.Sagas
{
    // ReSharper disable NotResolvedInText
    namespace UsingSagaReference
    {
        public class WhenCreatingSagaReference
        {
            [Fact]
            public void SagaTypeCannotBeNull()
            {
                Assert.Throws<ArgumentNullException>(() => new SagaReference(null, GuidStrategy.NewGuid()));
            }
        }

        public class WhenTestingEquality
        {
            [Fact]
            public void TypeAndIdMustBeEqual()
            {
                var correlationId = GuidStrategy.NewGuid();
                var reference1 = new SagaReference(typeof(Saga), correlationId);
                var reference2 = new SagaReference(typeof(Saga), correlationId);

                Assert.True(reference1.Equals(reference2));
            }

            [Fact]
            public void SagaReferenceCanBeBoxed()
            {
                var correlationId = GuidStrategy.NewGuid();
                var reference1 = new SagaReference(typeof(Saga), correlationId);
                var reference2 = new SagaReference(typeof(Saga), correlationId);

                Assert.True(reference1.Equals((Object)reference2));
            }
        }

        public class WhenGettingHashCode
        {
            [Fact]
            public void AlwaysReturnConsistentValue()
            {
                var correlationId = GuidStrategy.NewGuid();
                var reference1 = new SagaReference(typeof(Saga), correlationId);
                var reference2 = new SagaReference(typeof(Saga), correlationId);

                Assert.Equal(reference1.GetHashCode(), reference2.GetHashCode());
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnFriendlyDescription()
            {
                var correlationId = GuidStrategy.NewGuid();
                var reference = new SagaReference(typeof(Saga), correlationId);

                Assert.Equal(String.Format("{0} - {1}", typeof(Saga), correlationId), reference.ToString());
            }
        }
    }
}
