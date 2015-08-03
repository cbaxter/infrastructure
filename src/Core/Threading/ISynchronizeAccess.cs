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
    /// Provides a wrapper interface for <see cref="Monitor"/>.
    /// </summary>
    public interface ISynchronizeAccess
    {
        /// <summary>
        /// Notifies a thread in the waiting queue of a change in the locked object's state.
        /// </summary>
        /// <param name="obj">The object a thread is waiting for.</param>
        void Pulse(Object obj);

        /// <summary>
        /// Notifies all waiting threads of a change in the object's state.
        /// </summary>
        /// <param name="obj">The object a thread is waiting for.</param>
        void PulseAll(Object obj);

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock.
        /// </summary>
        /// <param name="obj">The object on which to wait.</param>
        /// <remarks>This method does not return if the lock is not reacquired.</remarks>
        void Wait(Object obj);

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock. If the specified time-out interval elapses, the thread enters the ready queue.
        /// </summary>
        /// <param name="obj">The object on which to wait.</param>
        /// <param name="timeout">A <see cref="TimeSpan"/> representing the amount of time to wait before the thread enters the ready queue.</param>
        /// <returns>
        /// <value>true</value> if the lock was reacquired before the specified time elapsed; <value>false</value> if the lock was reacquired after the specified time elapsed. 
        /// The method does not return until the lock is reacquired.
        /// </returns>
        Boolean Wait(Object obj, TimeSpan timeout);

        /// <summary>
        /// Releases the lock on an object and blocks the current thread until it reacquires the lock. If the specified time-out interval elapses, the thread enters the
        /// ready queue. Optionally exits the synchronization domain for the synchronized context before the wait and reacquires the domain afterward.
        /// </summary>
        /// <param name="obj">The object on which to wait.</param>
        /// <param name="timeout">A <see cref="TimeSpan"/> representing the amount of time to wait before the thread enters the ready queue.</param>
        /// <param name="exitContext"><value>true</value> to exit and reacquire the synchronization domain for the context (if in a synchronized context) before the wait; otherwise, <value>false</value>.</param>
        /// <returns>
        /// <value>true</value> if the lock was reacquired before the specified time elapsed; <value>false</value> if the lock was reacquired after the specified time elapsed. 
        /// The method does not return until the lock is reacquired.
        /// </returns>
        Boolean Wait(Object obj, TimeSpan timeout, Boolean exitContext);
    }
}
