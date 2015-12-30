using System;
using System.Collections.Generic;
using Moq;
using Spark;
using Xunit;

namespace Test.Spark
{
    namespace UsingObjectPool
    {
        public class WhenCreatingObjectPool
        {
            [Fact]
            public void WillDefaultCapacityToTwiceProcessorCount()
            {
                var capacity = Environment.ProcessorCount * 2;

                using (var objectPool = new ObjectPool<Object>(() => new Object()))
                    Assert.Equal(capacity, objectPool.Capacity);

                using (var objectPool = new ObjectPool<Object>(() => new Object(), _ => { }))
                    Assert.Equal(capacity, objectPool.Capacity);
            }

            [Fact]
            public void WillUseExplicitCapacityIfSpecified()
            {
                var capacity = 3;

                using (var objectPool = new ObjectPool<Object>(() => new Object(), capacity))
                    Assert.Equal(capacity, objectPool.Capacity);

                using (var objectPool = new ObjectPool<Object>(() => new Object(), _ => { }, capacity))
                    Assert.Equal(capacity, objectPool.Capacity);
            }
        }

        public class WhenAllocatingObjects
        {
            [Fact]
            public void WillAllocateNewObjectIfPoolEmpty()
            {
                var value = new Object();

                using (var objectPool = new ObjectPool<Object>(() => value))
                    Assert.Equal(value, objectPool.Allocate());
            }

            [Fact]
            public void WillAllocateExistingObjectIfPoolNotEmpty()
            {
                using (var objectPool = new ObjectPool<Object>(() => new Object()))
                {
                    var value = objectPool.Allocate();

                    objectPool.Free(value);

                    Assert.Equal(value, objectPool.Allocate());
                }
            }
        }

        public class WhenFreeingObjects
        {
            [Fact]
            public void WillReturnObjectToPoolIfExcessCapacity()
            {
                var value = new Mock<IDisposable>();

                using (var objectPool = new ObjectPool<IDisposable>(() => value.Object, item => item.Dispose()))
                {
                    objectPool.Free(objectPool.Allocate());

                    value.Verify(mock => mock.Dispose(), Times.Never());
                }
            }

            [Fact]
            public void WillDisposeObjectIfPoolCapacityExceeded()
            {
                var first = new Mock<IDisposable>();
                var second = new Mock<IDisposable>();
                var objects = new Queue<IDisposable>(new[] { first.Object, second.Object });

                using (var objectPool = new ObjectPool<IDisposable>(() => objects.Dequeue(), item => item.Dispose(), size: 1))
                {
                    var value1 = objectPool.Allocate();
                    var value2 = objectPool.Allocate();

                    objectPool.Free(value1);
                    objectPool.Free(value2);

                    first.Verify(mock => mock.Dispose(), Times.Never());
                    second.Verify(mock => mock.Dispose(), Times.Once());
                }
            }
        }

        public class WhenDisposing
        {
            [Fact]
            public void CanCallDisposeMoreThanOnce()
            {
                using (var objectPool = new ObjectPool<Object>(() => new Object()))
                    objectPool.Dispose();
            }

            [Fact]
            public void WillDisposeAllocatedItemsInPool()
            {
                var first = new Mock<IDisposable>();
                var second = new Mock<IDisposable>();
                var objects = new Queue<IDisposable>(new[] { first.Object, second.Object });

                using (var objectPool = new ObjectPool<IDisposable>(() => objects.Dequeue(), item => item.Dispose(), size: 2))
                {
                    var value1 = objectPool.Allocate();
                    var value2 = objectPool.Allocate();

                    objectPool.Free(value1);
                    objectPool.Free(value2);

                    first.Verify(mock => mock.Dispose(), Times.Never());
                    second.Verify(mock => mock.Dispose(), Times.Never());
                }

                first.Verify(mock => mock.Dispose(), Times.Once());
                second.Verify(mock => mock.Dispose(), Times.Once());
            }
        }
    }
}
