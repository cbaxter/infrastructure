using System;
using System.Diagnostics;
using Spark.Infrastructure.Threading;
using Xunit;

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

namespace Spark.Infrastructure.Tests.Threading
{
    public static class UsingExponentialBackoff
    {
        public class WhenCheckingCanRetry
        {
            [Fact]
            public void ReturnTrueIfSystemTimeLessThanTimeout()
            {
                var now = DateTime.UtcNow;

                SystemTime.OverrideWith(() => now);

                var backoff = new ExponentialBackoff(TimeSpan.FromMinutes(1));

                Assert.True(backoff.CanRetry);
            }

            [Fact]
            public void ReturnFalseIfSystemTimeGreaterThanTimeout()
            {
                var now = DateTime.UtcNow;

                SystemTime.OverrideWith(() => now);

                var backoff = new ExponentialBackoff(TimeSpan.FromMinutes(1));

                SystemTime.OverrideWith(() => now.AddMinutes(2));

                Assert.False(backoff.CanRetry);
            }
        }
        
        public class WehnWaitingUntilRetry
        {
            [Fact]
            public void FirstWaitShouldNotSleep()
            {
                SystemTime.ClearOverride();

                var backoff = new ExponentialBackoff(TimeSpan.FromMinutes(1));
                var stopwatch = Stopwatch.StartNew();
                
                backoff.WaitUntilRetry();
                stopwatch.Stop();

                Assert.InRange(stopwatch.ElapsedMilliseconds, 0, 2);
            }

            [Fact]
            public void SubsequentWaitsShouldSleep()
            {
                SystemTime.ClearOverride();

                var backoff = new ExponentialBackoff(TimeSpan.FromMinutes(1));

                backoff.WaitUntilRetry();

                var stopwatch = Stopwatch.StartNew();

                backoff.WaitUntilRetry();
                stopwatch.Stop();

                Assert.InRange(stopwatch.ElapsedMilliseconds, 8, 18);  
            }

            [Fact]
            public void SleepCannotExceedMaximumWait()
            {
                SystemTime.ClearOverride();

                var backoff = new ExponentialBackoff(TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(20));

                backoff.WaitUntilRetry();
                backoff.WaitUntilRetry();
                backoff.WaitUntilRetry();

                var stopwatch = Stopwatch.StartNew();

                backoff.WaitUntilRetry();
                stopwatch.Stop();

                Assert.InRange(stopwatch.ElapsedMilliseconds, 18, 38);  
            }

            [Fact]
            public void SleepCannotExceedTimeRemaining()
            {
                SystemTime.ClearOverride();

                var backoff = new ExponentialBackoff(TimeSpan.FromMilliseconds(25));

                backoff.WaitUntilRetry();
                backoff.WaitUntilRetry();

                var stopwatch = Stopwatch.StartNew();

                backoff.WaitUntilRetry();
                stopwatch.Stop();

                Assert.InRange(stopwatch.ElapsedMilliseconds, 12, 18);  
            }
        }
    }
}
