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
    /// Provides a mechanism for executing a method at specified intervals.
    /// </summary>
    public interface ITimer : IDisposable
    {
        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer, using <see cref="TimeSpan"/> values to measure time intervals.
        /// </summary>
        /// <param name="dueTime">A <see cref="TimeSpan"/> representing the amount of time to delay before invoking the callback method specified when the <see cref="Timer"/> was constructed. Specify negative one (-1) milliseconds to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback method specified when the <see cref="Timer"/> was constructed. Specify negative one (-1) milliseconds to disable periodic signaling.</param>
        void Change(TimeSpan dueTime, TimeSpan period);
        
        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer, using 64-bit signed integers to measure time intervals.
        /// </summary>
        /// <param name="dueTime">The amount of time to delay before the invoking the callback method specified when the <see cref="Timer"/> was constructed, in milliseconds. Specify <see cref="Timeout.Infinite"/> to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback method specified when the <see cref="Timer"/> was constructed, in milliseconds. Specify <see cref="Timeout.Infinite"/> to disable periodic signaling.</param>
        void Change(Int64 dueTime, Int64 period);
    }
}
