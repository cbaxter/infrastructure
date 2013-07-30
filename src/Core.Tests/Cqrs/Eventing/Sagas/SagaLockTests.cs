using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Spark;
using Spark.Cqrs.Eventing.Sagas;
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

namespace Test.Spark.Cqrs.Eventing.Sagas
{
    // ReSharper disable AccessToDisposedClosure
    public static class UsingSagaLock
    {
        public class WhenAquiringSagaLock
        {
            [Fact]
            public void CanAquireLockIfNoLockAquired()
            {
                using (var sagaLock = new SagaLock(typeof(Saga), GuidStrategy.NewGuid()))
                    Assert.DoesNotThrow(() => sagaLock.Aquire());
            }

            [Fact]
            public void CannotAquireLockIfLockAlreadyAquired()
            {
                var sagaId = GuidStrategy.NewGuid();
                using (var sagaLock = new SagaLock(typeof(Saga), sagaId))
                {
                    sagaLock.Aquire();
                    var ex = Assert.Throws<InvalidOperationException>(() => sagaLock.Aquire());

                    Assert.Equal(Exceptions.SagaLockAlreadyHeld.FormatWith(typeof(Saga), sagaId), ex.Message);
                }
            }

            [Fact]
            public void AquireWillBlockIfAnotherLockAlreadyAquiredOnSameSaga()
            {
                var correlationId = GuidStrategy.NewGuid();
                var firstLockAquired = new ManualResetEvent(initialState: false);
                var secondLockAquired = new ManualResetEvent(initialState: false);
                var blockedTime = TimeSpan.Zero;

                Task.Factory.StartNew(() =>
                    {
                        firstLockAquired.WaitOne();
                        using (var sagaLock = new SagaLock(typeof(Saga), correlationId))
                        {
                            var timer = Stopwatch.StartNew();

                            sagaLock.Aquire();
                            timer.Stop();

                            blockedTime = timer.Elapsed;
                            secondLockAquired.Set();
                        }
                    });

                using (var sagaLock = new SagaLock(typeof(Saga), correlationId))
                {
                    sagaLock.Aquire();
                    firstLockAquired.Set();

                    Thread.Sleep(100);
                }

                secondLockAquired.WaitOne();

                Assert.InRange(blockedTime, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(150));
            }

            [Fact]
            public void AquireWillNotBlockIfAnotherLockAlreadyAquiredOnAnotherSaga()
            {
                var firstLockAquired = new ManualResetEvent(initialState: false);
                var secondLockAquired = new ManualResetEvent(initialState: false);
                var blockedTime = TimeSpan.Zero;

                Task.Factory.StartNew(() =>
                    {
                        firstLockAquired.WaitOne();
                        using (var sagaLock = new SagaLock(typeof(Saga), GuidStrategy.NewGuid()))
                        {
                            var timer = Stopwatch.StartNew();

                            sagaLock.Aquire();
                            timer.Stop();

                            blockedTime = timer.Elapsed;
                            secondLockAquired.Set();
                        }
                    });

                using (var sagaLock = new SagaLock(typeof(Saga), GuidStrategy.NewGuid()))
                {
                    sagaLock.Aquire();
                    firstLockAquired.Set();

                    Thread.Sleep(100);
                }

                secondLockAquired.WaitOne();

                Assert.InRange(blockedTime, TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(50));
            }
        }

        public class WhenReleasingSagaLock
        {
            [Fact]
            public void CanReleaseLockIfLockAquired()
            {
                using (var sagaLock = new SagaLock(typeof(Saga), GuidStrategy.NewGuid()))
                {
                    sagaLock.Aquire();
                    Assert.DoesNotThrow(() => sagaLock.Release());
                }
            }

            [Fact]
            public void CannotReleaseLockIfLockNotAquired()
            {
                var sagaId = GuidStrategy.NewGuid();
                using (var sagaLock = new SagaLock(typeof(Saga), sagaId))
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => sagaLock.Release());

                    Assert.Equal(Exceptions.SagaLockNotHeld.FormatWith(typeof(Saga), sagaId), ex.Message);
                }
            }
        }

        public class WhenDisposingSagaLock
        {
            [Fact]
            public void CanDisposeIfLockAquired()
            {
                var sagaLock = new SagaLock(typeof(Saga), GuidStrategy.NewGuid());

                sagaLock.Aquire();

                sagaLock.Dispose();
            }

            [Fact]
            public void CanDisposeIfLockNotAquired()
            {
                var sagaLock = new SagaLock(typeof(Saga), GuidStrategy.NewGuid());

                sagaLock.Dispose();
            }

            [Fact]
            public void CanDisposeMoreThanOnce()
            {
                var sagaLock = new SagaLock(typeof(Saga), GuidStrategy.NewGuid());

                sagaLock.Aquire();
                sagaLock.Dispose();

                Assert.DoesNotThrow(() => sagaLock.Dispose());
            }
        }
    }
    // ReSharper restore AccessToDisposedClosure
}
