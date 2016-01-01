using System;
using System.Collections.Generic;
using System.Linq;

namespace Spark
{
    /// <summary>
    /// Generic implementation of an object pool that may be used to limited the number of frequently used objects that will be created/destroyed.
    /// </summary>
    /// <remarks>
    /// This object pool implementation does not guarantee an allocated object will be stored; during periods of high contention,
    /// the pool may be overallocated, and once freed any object that does not fit within the specified capacity will be released.
    /// </remarks>
    public class ObjectPool<T> : IDisposable
        where T : class
    {
        private readonly Queue<T> pool;
        private readonly Action<T> dispose;
        private readonly Func<T> factory;
        private readonly Int32 capacity;

        /// <summary>
        /// The maximum stored object pool capacity.
        /// </summary>
        public Int32 Capacity => capacity;

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectPool{T}"/> using the specified object <paramref name="factory"/>.
        /// </summary>
        /// <param name="factory">The factory method used to allocate new instances of the underlying pooled object.</param>
        public ObjectPool(Func<T> factory)
            : this(factory, null, Environment.ProcessorCount * 2)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectPool{T}"/> using the specified object <paramref name="factory"/> and <paramref name="size"/>.
        /// </summary>
        /// <param name="factory">The factory method used to allocate new instances of the underlying pooled object.</param>
        /// <param name="size">The maximum stored object pool capacity.</param>
        public ObjectPool(Func<T> factory, Int32 size)
            : this(factory, null, size)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectPool{T}"/> using the specified object <paramref name="factory"/> and <paramref name="dispose"/> methods.
        /// </summary>
        /// <param name="factory">The factory method used to allocate new instances of the underlying pooled object.</param>
        /// <param name="dispose">The dispose method used to free overallocated instances of the underlying pooled object.</param>
        public ObjectPool(Func<T> factory, Action<T> dispose)
            : this(factory, dispose, Environment.ProcessorCount * 2)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectPool{T}"/> using the specified object <paramref name="factory"/> and <paramref name="dispose"/> methods.
        /// </summary>
        /// <param name="factory">The factory method used to allocate new instances of the underlying pooled object.</param>
        /// <param name="dispose">The dispose method used to free overallocated instances of the underlying pooled object.</param>
        /// <param name="size">The maximum stored object pool capacity.</param>
        public ObjectPool(Func<T> factory, Action<T> dispose, Int32 size)
        {
            Verify.NotNull(factory, nameof(factory));
            Verify.GreaterThan(0, size, nameof(size));

            this.capacity = size;
            this.factory = factory;
            this.dispose = dispose;
            this.pool = new Queue<T>(capacity: size);
        }

        /// <summary>
        /// Gets a pooled instance of <typeparamref name="T"/> or creates a new instance if the pool is empty.
        /// </summary>
        public T Allocate()
        {
            lock (pool)
            {
                if (pool.Count > 0)
                    return pool.Dequeue();
            }

            return factory.Invoke();
        }
        /// <summary>
        /// Returns an allocated instance of <typeparamref name="T"/> and returns to the object pool if space is available.
        /// </summary>
        public void Free(T item)
        {
            lock (pool)
            {
                if (pool.Count < capacity)
                {
                    pool.Enqueue(item);
                    return;
                }
            }

            dispose?.Invoke(item);
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="ObjectPool{T}"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="ObjectPool{T}"/> class.
        /// </summary>
        /// <param name="disposing">Indicates if managed resources are being disposed.</param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                List<T> items;

                lock (pool)
                {
                    items = pool.ToList();
                    pool.Clear();
                }

                items.ForEach(item => dispose?.Invoke(item));
            }
        }
    }
}
