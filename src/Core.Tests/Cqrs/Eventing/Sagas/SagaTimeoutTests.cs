using System;
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
    namespace UsingSagaTimeout
    {
        public class WhenCreatingTimeout
        {
            [Fact]
            public void SagaTypeCannotBeNull()
            {
                Assert.Throws<ArgumentNullException>(() => new SagaTimeout(null, Guid.Empty, DateTime.UtcNow));
            }
        }

        public class WhenTestingEquality
        {
            [Fact]
            public void NotEqualIfSagaTypesDiffer()
            {
                var now = DateTime.UtcNow;
                var sagaId = Guid.NewGuid();
                var lhs = new SagaTimeout(typeof(Saga1), sagaId, now);
                var rhs = new SagaTimeout(typeof(Saga2), sagaId, now);

                Assert.NotEqual(lhs, rhs);
            }

            [Fact]
            public void NotEqualIfSagaIdsDiffer()
            {
                var now = DateTime.UtcNow;
                var lhs = new SagaTimeout(typeof(Saga1), Guid.NewGuid(), now);
                var rhs = new SagaTimeout(typeof(Saga1), Guid.NewGuid(), now);

                Assert.NotEqual(lhs, rhs);
            }

            [Fact]
            public void NotEqualIfTimeoutsDiffer()
            {
                var now = DateTime.UtcNow;
                var sagaId = Guid.NewGuid();
                var lhs = new SagaTimeout(typeof(Saga1), sagaId, now);
                var rhs = new SagaTimeout(typeof(Saga1), sagaId, now.AddMilliseconds(1));

                Assert.NotEqual(lhs, rhs);
            }

            [Fact]
            public void EqualIfSameSagaTypeAndIdAndTimeout()
            {
                var now = DateTime.UtcNow;
                var sagaId = Guid.NewGuid();
                var lhs = new SagaTimeout(typeof(Saga1), sagaId, now);
                var rhs = new SagaTimeout(typeof(Saga1), sagaId, now);

                Assert.Equal(lhs, rhs);
            }

            private sealed class Saga1 { }
            private sealed class Saga2 { }
        }
    }
}
