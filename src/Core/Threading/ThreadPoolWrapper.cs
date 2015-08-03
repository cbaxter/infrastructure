using System;
using System.Threading;

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
