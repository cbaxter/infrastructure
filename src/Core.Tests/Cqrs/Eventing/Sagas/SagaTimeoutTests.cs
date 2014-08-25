using System;
using Spark.Cqrs.Eventing.Sagas;
using Xunit;

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
