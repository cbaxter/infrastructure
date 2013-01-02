using System.Collections;
using System.Collections.Generic;
using System.Text;
using Spark.Infrastructure.Logging;
using Spark.Infrastructure.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
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
    public class PartitionedTaskSchedulerTests
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        [Fact]
        public void Test()
        {
            var scheduler = new PartitionedTaskScheduler(t => t.Id.GetHashCode(), 10, 50);
            var tasks = new Task[1000];

            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Factory.StartNew(() =>
                    {
                        var inline = new Task(() =>
                            {
                                Console.WriteLine("Synchronous..." + Thread.CurrentThread.ManagedThreadId); Thread.Sleep(1);
                            });

                        inline.RunSynchronously();
                        Console.WriteLine(Thread.CurrentThread.ManagedThreadId); Thread.Sleep(1);
                    }, CancellationToken.None, TaskCreationOptions.None, scheduler).ContinueWith(t => { Console.WriteLine("Continued..." + Thread.CurrentThread.ManagedThreadId); Thread.Sleep(1); }, TaskContinuationOptions.None);
            }

            Task.WaitAll(tasks);

            new Task(() => { Console.WriteLine(Thread.CurrentThread.ManagedThreadId); Thread.Sleep(1); }).RunSynchronously(scheduler);
        }


        [Fact]
        public void TestExampleWithRunSynchronously()//Bad
        {
            var scheduler = new PartitionedTaskScheduler(t => t.Id.GetHashCode(), 10, 50);
            var tasks = new Task[1000];

            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Factory.StartNew(() => new Task(() => Thread.Sleep(1)).RunSynchronously(), CancellationToken.None, TaskCreationOptions.None, scheduler);
            }

            Task.WaitAll(tasks);
        }

        [Fact]
        public void TestExampleWithWait()//Bad
        {
            var scheduler = new PartitionedTaskScheduler(t => t.Id.GetHashCode(), 100, 500);
            var tasks = new Task[1000];

            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Factory.StartNew(() =>
                {
                    var task = new Task(() => Thread.Sleep(1));
                    task.Start();
                    Thread.Sleep(1);
                    task.Wait();
                }, CancellationToken.None, TaskCreationOptions.None, scheduler);
            }

            Task.WaitAll(tasks);
        }

        [Fact]
        public void TestExampleWithParentWait() //Good
        {
            var scheduler = new PartitionedTaskScheduler(t => t.Id.GetHashCode(), 10, 50);
            var task = Task.Factory.StartNew(() =>
                {
                    for (var i = 0; i < 1000; i++)
                    {
                        var x = i;
                        Task.Factory.StartNew(() => { Console.WriteLine(x); Thread.Sleep(1); }, CancellationToken.None, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning, scheduler);
                    }
                });



            Object value = new Func<String>(() => "");

            value = UnwrapIfRequired(value);

            task.Wait();
        }

        [Fact]
        public void Benchmark()
        {
            Verify.GreaterThan(1, 1, "test");

            var start = DateTime.Now;
            var x = new List<String>();
            var data = new object[] {1, 2};
            var name = "Task";

            for (var i = 0; i < 100000; i++)
            {
                // NONE             -> 00:00:00.0010001
                // NULL SINGLETON   -> 00:00:00.0030001
                // NULL INSTANCE    -> 00:00:00.0050003
                // ACTIVITY ID      -> 00:00:00.0420024
                // LOGICAL OPS      -> 00:00:00.0840048
                // FULL             -> 00:00:00.2820161

                using (Log.PushContext("Task", i, i))
                {
                    Log.Trace("Queuing user work item on thread-pool.");
                }
            }

            var elapsed = DateTime.Now.Subtract(start);

            Console.WriteLine(elapsed);
        }


        private Object UnwrapIfRequired<T>(T value)
        {
            var wrappedValue = value as Func<Object>;

            return wrappedValue == null ? value : wrappedValue();
        }
    }
}
