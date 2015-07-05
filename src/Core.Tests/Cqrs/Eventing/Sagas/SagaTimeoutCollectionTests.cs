using System;
using System.Linq;
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
    namespace UsingSagaTimeoutCollection
    {
        public class WhenAddingSagaTimeout
        {
            private readonly SagaTimeoutCollection collection = new SagaTimeoutCollection();

            [Fact]
            public void CanAddFirstTimeoutForSaga()
            {
                var timeout = new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow);

                collection.Add(timeout);

                Assert.True(collection.Contains(timeout));
            }

            [Fact]
            public void CanAddSubsequentTimeoutsForSameSaga()
            {
                var sagaType = typeof(Saga);
                var sagaId = Guid.NewGuid();
                var timeout1 = new SagaTimeout(sagaType, sagaId, DateTime.UtcNow);
                var timeout2 = new SagaTimeout(sagaType, sagaId, DateTime.UtcNow.AddSeconds(2));

                collection.Add(timeout1);
                collection.Add(timeout2);

                Assert.True(collection.Contains(timeout1));
                Assert.True(collection.Contains(timeout2));
            }

            [Fact]
            public void CanAddTimeoutsForMultipleSagas()
            {
                var timeout1 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow);
                var timeout2 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow.AddSeconds(1));

                collection.Add(timeout1);
                collection.Add(timeout2);

                Assert.True(collection.Contains(timeout1));
                Assert.True(collection.Contains(timeout2));
            }

            [Fact]
            public void CanAddTwoIdenticalTimeouts()
            {
                var now = DateTime.UtcNow;
                var sagaType = typeof(Saga);
                var sagaId = Guid.NewGuid();
                var timeout1 = new SagaTimeout(sagaType, sagaId, now);
                var timeout2 = new SagaTimeout(sagaType, sagaId, now);

                collection.Add(timeout1);
                collection.Add(timeout2);

                Assert.Equal(2, collection.Count);
            }

            [Fact]
            public void AddTimeoutsInChronologicalOrder()
            {
                var timeout1 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow);
                var timeout2 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow.AddSeconds(1));
                var timeout3 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow.AddSeconds(1));
                var timeout4 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow.AddMinutes(1));

                collection.Add(timeout4);
                collection.Add(timeout2);
                collection.Add(timeout1);
                collection.Add(timeout3);

                var items = collection.ToArray();

                Assert.Equal(timeout1, items[0]);
                Assert.Equal(timeout2, items[1]);
                Assert.Equal(timeout3, items[2]);
                Assert.Equal(timeout4, items[3]);
            }
        }

        public class WhenClearingSagaTimeouts
        {
            [Fact]
            public void RemoveAllSagaTimeouts()
            {
                var collection = new SagaTimeoutCollection();

                collection.Add(new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow));
                collection.Add(new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow));
                collection.Add(new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow));

                collection.Clear();

                Assert.Empty(collection);
            }
        }

        public class WhenRemovingSagaReferenceTimeouts
        {
            private readonly SagaTimeoutCollection collection = new SagaTimeoutCollection();

            [Fact]
            public void DoNotRemoveAnySagaTimeoutsIfSagaReferenceNotFound()
            {
                Assert.False(collection.Remove(new SagaReference(typeof(Saga), Guid.NewGuid())));
            }

            [Fact]
            public void RemoveAllSagaTimeoutsForSpecifiedSagaReference()
            {
                var sagaReference = new SagaReference(typeof(Saga), Guid.NewGuid());
                var timeout1 = new SagaTimeout(typeof(Saga), sagaReference.SagaId, DateTime.UtcNow);
                var timeout2 = new SagaTimeout(typeof(Saga), sagaReference.SagaId, DateTime.UtcNow.AddSeconds(1));

                collection.Add(timeout1);
                collection.Add(timeout2);
                collection.Remove(sagaReference);

                Assert.False(collection.Contains(timeout1));
                Assert.False(collection.Contains(timeout2));
                Assert.False(collection.Contains(sagaReference));
            }

            [Fact]
            public void DoNotRemoveSagaTimeoutsForAnotherSagaReference()
            {
                var timeout1 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow);
                var timeout2 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow);

                collection.Add(timeout1);
                collection.Add(timeout2);
                collection.Remove((SagaReference)timeout2);

                Assert.True(collection.Contains(timeout1));
                Assert.False(collection.Contains(timeout2));
            }
        }

        public class WhenRemovingSpecificSagaTimeout
        {
            private readonly SagaTimeoutCollection collection = new SagaTimeoutCollection();

            [Fact]
            public void DoNotRemoveAnySagaTimeoutsIfSagaReferenceNotFound()
            {
                var timeout = new SagaTimeout(typeof(Saga), Guid.NewGuid(), DateTime.UtcNow);

                Assert.False(collection.Remove(timeout));
            }

            [Fact]
            public void RemoveFirstInstanceOfSagaTimeout()
            {
                var now = DateTime.UtcNow;
                var timeout1 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), now);
                var timeout2 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), now);
                var timeout3 = new SagaTimeout(typeof(Saga), timeout2.SagaId, timeout2.Timeout);
                var timeout4 = new SagaTimeout(typeof(Saga), timeout2.SagaId, now.AddMilliseconds(1));

                collection.Add(timeout1);
                collection.Add(timeout2);
                collection.Add(timeout3);
                collection.Add(timeout4);

                Assert.True(collection.Remove(timeout3));
                Assert.True(collection.Contains(timeout2));
            }

            [Fact]
            public void RemoveSagaReferenceIfLastSagaTimeout()
            {
                var now = DateTime.UtcNow;
                var timeout1 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), now);
                var timeout2 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), now);

                collection.Add(timeout1);
                collection.Add(timeout2);

                Assert.True(collection.Remove(timeout2));
                Assert.False(collection.Contains((SagaReference)timeout2));
            }

            [Fact]
            public void RemoveAllInstanceOfSagaTimeoutWhenRequested()
            {
                var now = DateTime.UtcNow;
                var timeout1 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), now);
                var timeout2 = new SagaTimeout(typeof(Saga), Guid.NewGuid(), now);
                var timeout3 = new SagaTimeout(typeof(Saga), timeout2.SagaId, timeout2.Timeout);
                var timeout4 = new SagaTimeout(typeof(Saga), timeout2.SagaId, now.AddMilliseconds(1));

                collection.Add(timeout1);
                collection.Add(timeout2);
                collection.Add(timeout3);
                collection.Add(timeout4);

                Assert.True(collection.RemoveAll(timeout3));
                Assert.False(collection.Contains(timeout2));
            }
        }
    }
}
