using System;
using System.Diagnostics;
using System.Threading;

/* Copyright (c) 2013 Spark Software Ltd.
 * 
 * This source is subject to the GNU Lesser General Public License.
 * See: http://www.gnu.org/copyleft/lesser.html
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE. 
 */

namespace Spark.Threading
{
    /// <summary>
    /// Provides a mechanism for executing a method at specified intervals.
    /// </summary>
    public sealed class TimerWrapper : ITimer
    {
        private readonly Timer timer;

        /// <summary>
        /// Initializes a new instance of the Timer class, using <see cref="TimeSpan"/> values to measure time intervals.
        /// </summary>
        /// <param name="callback">A <see cref="TimerCallback"/> delegate representing a method to be executed.</param>
        /// <param name="state">An object containing information to be used by the callback method, or null.</param>
        /// <param name="dueTime">The amount of time to delay before <paramref name="callback"/> is invoked, in milliseconds. Specify <see cref="Timeout.Infinite"/> to prevent the timer from starting. Specify zero (0) to start the timer immediately. </param>
        /// <param name="period">The time interval between invocations of <paramref name="callback"/>, in milliseconds. Specify <see cref="Timeout.Infinite"/> to disable periodic signaling. </param>
        public TimerWrapper(TimerCallback callback, Object state, TimeSpan dueTime, TimeSpan period)
            : this(callback, state, dueTime.Ticks / TimeSpan.TicksPerMillisecond, period.Ticks / TimeSpan.TicksPerMillisecond)
        {
            timer = new Timer(callback, state, dueTime, period);
        }

        /// <summary>
        /// Initializes a new instance of the Timer class, using 64 bit unsigned integer values to measure time intervals.
        /// </summary>
        /// <param name="callback">A <see cref="TimerCallback"/> delegate representing a method to be executed.</param>
        /// <param name="state">An object containing information to be used by the callback method, or null.</param>
        /// <param name="dueTime">The amount of time to delay before <paramref name="callback"/> is invoked, in milliseconds. Specify <see cref="Timeout.Infinite"/> to prevent the timer from starting. Specify zero (0) to start the timer immediately. </param>
        /// <param name="period">The time interval between invocations of <paramref name="callback"/>, in milliseconds. Specify <see cref="Timeout.Infinite"/> to disable periodic signaling. </param>
        public TimerWrapper(TimerCallback callback, Object state, Int64 dueTime, Int64 period)
        {
            timer = new Timer(callback, state, dueTime, period);
        }

        /// <summary>
        /// Releases all resources used by the current instance of <see cref="Timer"/> and signals when the timer has been disposed of.
        /// </summary>
        public void Dispose()
        {
            timer.Dispose();
        }

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer, using <see cref="TimeSpan"/> values to measure time intervals.
        /// </summary>
        /// <param name="dueTime">A <see cref="TimeSpan"/> representing the amount of time to delay before invoking the callback method specified when the <see cref="Timer"/> was constructed. Specify negative one (-1) milliseconds to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback method specified when the <see cref="Timer"/> was constructed. Specify negative one (-1) milliseconds to disable periodic signaling.</param>
        public void Change(TimeSpan dueTime, TimeSpan period)
        {
            Change(dueTime.Ticks / TimeSpan.TicksPerMillisecond, period.Ticks / TimeSpan.TicksPerMillisecond);
        }

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer, using 64-bit signed integers to measure time intervals.
        /// </summary>
        /// <param name="dueTime">The amount of time to delay before the invoking the callback method specified when the <see cref="Timer"/> was constructed, in milliseconds. Specify <see cref="Timeout.Infinite"/> to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback method specified when the <see cref="Timer"/> was constructed, in milliseconds. Specify <see cref="Timeout.Infinite"/> to disable periodic signaling.</param>
        public void Change(Int64 dueTime, Int64 period)
        {
            //NOTE: Although Change is typed as returning a bool, it will actually never return anything but true. If there is a problem changing the
            //      timer-such as the target object already having been deleted-an exception will be thrown. See Concurrent Programming on Windows p373.
            Debug.Assert(timer.Change(dueTime, period));
        }
    }
}
