using System;
using System.Threading;

/* Copyright (c) 2012 Spark Software Ltd.
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

namespace Spark.Infrastructure.Threading
{
    /// <summary>
    /// Provides a exponential backoff state object for delaying retries.
    /// </summary>
    public sealed class ExponentialBackoff
    {
        private readonly TimeSpan maximumWait;
        private readonly DateTime endTime;
        private TimeSpan wait;

        /// <summary>
        /// Returns <value>true</value> if the backoff window has not lapsed; otherwise <value>false</value>.
        /// </summary>
        public Boolean CanRetry { get { return SystemTime.Now < endTime; } }

        /// <summary>
        /// Construct a new exponential backoff context with the specified <paramref name="timeout"/>.
        /// </summary>
        /// <param name="timeout">The time until can no longer allow retry attempts.</param>
        public ExponentialBackoff(TimeSpan timeout)
            : this(timeout, TimeSpan.MaxValue)
        { }

        /// <summary>
        /// Construct a new exponential backoff context with the specified <paramref name="timeout"/>.
        /// </summary>
        /// <param name="timeout">The time until can no longer allow retry attempts.</param>
        /// <param name="maximumWait">The maximum sleep time between retry attempts.</param>
        public ExponentialBackoff(TimeSpan timeout, TimeSpan maximumWait)
        {
            this.endTime = SystemTime.Now.Add(timeout);
            this.maximumWait = maximumWait;
            this.wait = TimeSpan.Zero;
        }

        /// <summary>
        /// Blocks the current thread until the next retry attempt should be made.
        /// </summary>
        /// <remarks>The first retry will always be immediate; subsequent retries will increase exponentially starting from 10ms.</remarks>
        public void WaitUntilRetry()
        {
            if (wait == TimeSpan.Zero)
            {
                wait = TimeSpan.FromMilliseconds(5);
            }
            else
            {
                wait = TimeSpan.FromMilliseconds(wait.TotalMilliseconds * 2);
                if (wait > maximumWait)
                    wait = maximumWait;

                var timeRemaining = endTime.Subtract(SystemTime.Now);
                if (wait > timeRemaining)
                    wait = timeRemaining;

                if(wait > TimeSpan.Zero)
                    Thread.Sleep(wait);
            }
        }
    }
}
