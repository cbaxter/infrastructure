using System;
using System.Threading;

namespace Spark.Threading
{
    /// <summary>
    /// Provides a instance wrapper for <see cref="ThreadPool"/>.
    /// </summary>
    public sealed class ThreadPoolWrapper : IQueueUserWorkItems
    {
        /// <summary>
        /// A default instance of <see cref="ThreadPoolWrapper"/>.
        /// </summary>
        public static readonly ThreadPoolWrapper Instance = new ThreadPoolWrapper();
        
        /// <summary>
        /// Initializes a new instance of <see cref="ThreadPoolWrapper"/>.
        /// </summary>
        private ThreadPoolWrapper()
        { }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="action">An <see cref="Action"/> that represents the method to be executed.</param>
        public void QueueUserWorkItem(Action action)
        {
            Verify.NotNull(action, "action");

            ThreadPool.QueueUserWorkItem(_ => action());
        }

        /// <summary>
        /// Queues a method for execution and specifies an object containing data to be used by the method. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <typeparam name="T">The type of state passed in to <paramref name="action"/>.</typeparam>
        /// <param name="action">An <see cref="Action"/> that represents the method to be executed.</param>
        /// <param name="state">An object containing data to be used by the method.</param>
        public void QueueUserWorkItem<T>(Action<T> action, T state)
        {
            Verify.NotNull(action, "action");

            ThreadPool.QueueUserWorkItem(s => action((T)s), state);
        }

        /// <summary>
        /// Queues a method for execution, but does not propagate the calling stack to the worker thread. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="action">An <see cref="Action"/> that represents the method to be executed.</param>
        public void UnsafeQueueUserWorkItem(Action action)
        {
            Verify.NotNull(action, "action");

            ThreadPool.UnsafeQueueUserWorkItem(_ => action(), null);
        }

        /// <summary>
        /// Queues a method for execution and specifies an object containing data to be used by the method, but does not 
        /// propagate the calling stack to the worker thread. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <typeparam name="T">The type of state passed in to <paramref name="action"/>.</typeparam>
        /// <param name="action">An <see cref="Action"/> that represents the method to be executed.</param>
        /// <param name="state">An object containing data to be used by the method.</param>
        public void UnsafeQueueUserWorkItem<T>(Action<T> action, T state)
        {
            Verify.NotNull(action, "action");

            ThreadPool.UnsafeQueueUserWorkItem(s => action((T)s), state);
        }
    }
}
