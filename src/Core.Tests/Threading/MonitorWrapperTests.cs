using System;
using System.Threading;
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
    public static class UsingSMonitorWrapper
    {
        public class WhenWaiting
        {
            [Fact]
            public void WaitUntilPulsed()
            {
                var syncLock = new Object();

                ThreadPool.QueueUserWorkItem(_ => { lock (syncLock) { Monitor.Pulse(syncLock); } });

                lock (syncLock)
                {
                    MonitorWrapper.Instance.Wait(syncLock);
                }
            }

            [Fact]
            public void WaitUntilTimeout()
            {
                var syncLock = new Object();

                lock (syncLock)
                {
                    Assert.False(MonitorWrapper.Instance.Wait(syncLock, TimeSpan.FromMilliseconds(10)));
                }
            }

            [Fact]
            public void WaitUntilTimeoutWithExitContext()
            {
                var syncLock = new Object();

                lock (syncLock)
                {
                    Assert.False(MonitorWrapper.Instance.Wait(syncLock, TimeSpan.FromMilliseconds(10), true));
                }
            }
        }

        public class WhenPulsingOne
        {
            [Fact]
            public void WaitUntilPulsed()
            {
                var syncLock = new Object();

                ThreadPool.QueueUserWorkItem(_ => { lock (syncLock) { MonitorWrapper.Instance.Pulse(syncLock); } });
                
                lock (syncLock)
                {
                    Monitor.Wait(syncLock);
                }
            }

            [Fact]
            public void WaitUntilAllPulsed()
            {
                var syncLock = new Object();

                ThreadPool.QueueUserWorkItem(_ => { lock (syncLock) { MonitorWrapper.Instance.PulseAll(syncLock); } });

                lock (syncLock)
                {
                    Monitor.Wait(syncLock);
                }
            }
        }
    }
}
