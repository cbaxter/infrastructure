using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Spark;
using Spark.Cqrs.Domain;
using Spark.Resources;
using Xunit;

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

namespace Test.Spark.Cqrs.Domain
{
    // ReSharper disable AccessToDisposedClosure
    namespace UsingAggregateLock
    {
        public class WhenAquiringAggregateLock
        {
            [Fact]
            public void CanAquireLockIfNoLockAquired()
            {
                using (var aggregateLock = new AggregateLock(typeof(Aggregate), GuidStrategy.NewGuid()))
                {
                    aggregateLock.Aquire();

                    Assert.True(aggregateLock.Aquired);
                }
            }

            [Fact]
            public void CannotAquireLockIfLockAlreadyAquired()
            {
                var aggregateId = GuidStrategy.NewGuid();
                using (var aggregateLock = new AggregateLock(typeof(Aggregate), aggregateId))
                {
                    aggregateLock.Aquire();
                    var ex = Assert.Throws<InvalidOperationException>(() => aggregateLock.Aquire());

                    Assert.Equal(Exceptions.AggregateLockAlreadyHeld.FormatWith(typeof(Aggregate), aggregateId), ex.Message);
                }
            }

            [Fact]
            public void AquireWillBlockIfAnotherLockAlreadyAquiredOnSameAggregate()
            {
                var correlationId = GuidStrategy.NewGuid();
                var firstLockAquired = new ManualResetEvent(initialState: false);
                var secondLockAquired = new ManualResetEvent(initialState: false);
                var blockedTime = TimeSpan.Zero;

                Task.Factory.StartNew(() =>
                {
                    firstLockAquired.WaitOne();
                    using (var aggregateLock = new AggregateLock(typeof(Aggregate), correlationId))
                    {
                        var timer = Stopwatch.StartNew();

                        aggregateLock.Aquire();
                        timer.Stop();

                        blockedTime = timer.Elapsed;
                        secondLockAquired.Set();
                    }
                });

                using (var aggregateLock = new AggregateLock(typeof(Aggregate), correlationId))
                {
                    aggregateLock.Aquire();
                    firstLockAquired.Set();

                    Thread.Sleep(100);
                }

                secondLockAquired.WaitOne();

                Assert.InRange(blockedTime, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(150));
            }

            [Fact]
            public void AquireWillNotBlockIfAnotherLockAlreadyAquiredOnAnotherAggregate()
            {
                var firstLockAquired = new ManualResetEvent(initialState: false);
                var secondLockAquired = new ManualResetEvent(initialState: false);
                var blockedTime = TimeSpan.Zero;

                Task.Factory.StartNew(() =>
                {
                    firstLockAquired.WaitOne();
                    using (var aggregateLock = new AggregateLock(typeof(Aggregate), GuidStrategy.NewGuid()))
                    {
                        var timer = Stopwatch.StartNew();

                        aggregateLock.Aquire();
                        timer.Stop();

                        blockedTime = timer.Elapsed;
                        secondLockAquired.Set();
                    }
                });

                using (var aggregateLock = new AggregateLock(typeof(Aggregate), GuidStrategy.NewGuid()))
                {
                    aggregateLock.Aquire();
                    firstLockAquired.Set();

                    Thread.Sleep(100);
                }

                secondLockAquired.WaitOne();

                Assert.InRange(blockedTime, TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(50));
            }
        }

        public class WhenReleasingAggregateLock
        {
            [Fact]
            public void CanReleaseLockIfLockAquired()
            {
                using (var aggregateLock = new AggregateLock(typeof(Aggregate), GuidStrategy.NewGuid()))
                {
                    aggregateLock.Aquire();
                    aggregateLock.Release();

                    Assert.False(aggregateLock.Aquired);
                }
            }

            [Fact]
            public void CannotReleaseLockIfLockNotAquired()
            {
                var aggregateId = GuidStrategy.NewGuid();
                using (var aggregateLock = new AggregateLock(typeof(Aggregate), aggregateId))
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => aggregateLock.Release());

                    Assert.Equal(Exceptions.AggregateLockNotHeld.FormatWith(typeof(Aggregate), aggregateId), ex.Message);
                }
            }
        }

        public class WhenDisposingAggregateLock
        {
            [Fact]
            public void CanDisposeIfLockAquired()
            {
                var aggregateLock = new AggregateLock(typeof(Aggregate), GuidStrategy.NewGuid());

                aggregateLock.Aquire();

                aggregateLock.Dispose();
            }

            [Fact]
            public void CanDisposeIfLockNotAquired()
            {
                var aggregateLock = new AggregateLock(typeof(Aggregate), GuidStrategy.NewGuid());

                aggregateLock.Dispose();
            }

            [Fact]
            public void CanDisposeMoreThanOnce()
            {
                var aggregateLock = new AggregateLock(typeof(Aggregate), GuidStrategy.NewGuid());

                aggregateLock.Aquire();
                aggregateLock.Dispose();

                aggregateLock.Dispose();
            }
        }
    }
    // ReSharper restore AccessToDisposedClosure
}