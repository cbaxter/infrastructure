using Spark.Infrastructure.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public static class UsingPartitionedTaskScheduler
    {
        public class WhenInitializing
        {
            [Fact]
            public void SetMaximumConcurrencyLevelToNumberOfWorkerThreads()
            {
                Int32 workerThreads, completionPortThreads;
                ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

                Assert.Equal(workerThreads, new PartitionedTaskScheduler().MaximumConcurrencyLevel);
            }

            [Fact]
            public void DefaultMaximumQueuedTasksToNumberOfWorkerThreads()
            {
                Int32 workerThreads, completionPortThreads;
                ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

                Assert.Equal(workerThreads, new PartitionedTaskScheduler().BoundedCapacity);
            }
        }

        public class WhenQueuingTask
        {
            [Fact]
            public void QueueImmediatelyIfBelowBoundedCapacity()
            {
                var taskScheduler = new PartitionedTaskScheduler(_ => 0, 1);
                var task = Task.Factory.StartNew(() =>
                    {
                        Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler);
                    });

                Assert.True(task.Wait(TimeSpan.FromMilliseconds(100)));
                Assert.Equal(0, taskScheduler.ScheduledTasks.Count());
            }

            [Fact]
            public void BlockQueueUntilBelowBoundedCapacity()
            {
                var monitor = new FakeMonitor();
                var threadPool = new FakeThreadPool();
                var blocked = new ManualResetEvent(false);
                var released = new ManualResetEvent(false);
                var taskScheduler = new PartitionedTaskScheduler(_ => 0, 1, 1, threadPool, monitor);

                monitor.BeforeWait = () => blocked.Set();
                monitor.AfterPulse = () => released.Set();

                Task.Factory.StartNew(() =>
                {
                    // Schedule first task (non-blocking).
                    Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler);

                    // Schedule second task (blocking).
                    Task.Factory.StartNew(() => { }, CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler);
                });

                // Wait for second task to be blocked.
                Assert.True(blocked.WaitOne(TimeSpan.FromMilliseconds(100)));
                Assert.Equal(1, threadPool.UserWorkItems.Count);

                threadPool.RunNext();

                // Wait for second task to be released.
                Assert.True(released.WaitOne(TimeSpan.FromMilliseconds(100)));

                threadPool.RunNext();

                Assert.Equal(0, taskScheduler.ScheduledTasks.Count());
            }

            [Fact]
            public void RunOnDedicatedThreadIfLongRunning()
            {
                var isThreadPool = false;
                var taskComplete = new ManualResetEvent(false);
                var taskScheduler = new PartitionedTaskScheduler();

                Task.Factory.StartNew(() => { isThreadPool = Thread.CurrentThread.IsThreadPoolThread; taskComplete.Set(); }, CancellationToken.None, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning, taskScheduler);

                Assert.True(taskComplete.WaitOne(TimeSpan.FromMilliseconds(100)));
                Assert.False(isThreadPool);
            }

            [Fact]
            public void RunOnBackgroundThreadIfLongRunning()
            {
                var isBackground = false;
                var taskComplete = new ManualResetEvent(false);
                var taskScheduler = new PartitionedTaskScheduler();

                Task.Factory.StartNew(() => { isBackground = Thread.CurrentThread.IsBackground; taskComplete.Set(); }, CancellationToken.None, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning, taskScheduler);

                Assert.True(taskComplete.WaitOne(TimeSpan.FromMilliseconds(100)));
                Assert.True(isBackground);
            }

            [Fact]
            public void RunOnThreadPoolThreadIfNotLongRunning()
            {
                var isThreadPool = false;
                var taskComplete = new ManualResetEvent(false);
                var taskScheduler = new PartitionedTaskScheduler();

                Task.Factory.StartNew(() => { isThreadPool = Thread.CurrentThread.IsThreadPoolThread; taskComplete.Set(); }, CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler);

                Assert.True(taskComplete.WaitOne(TimeSpan.FromMilliseconds(100)));
                Assert.True(isThreadPool);
            }

            [Fact]
            public void RunOnSameThreadIfAdditionalTaskQueuedOnSamePartition()
            {
                var threadIds = new List<Int32>();
                var tasksQueued = new ManualResetEvent(false);
                var taskScheduler = new PartitionedTaskScheduler();

                var task1 = Task.Factory.StartNew(() => { threadIds.Add(Thread.CurrentThread.ManagedThreadId); tasksQueued.WaitOne(); }, CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler);
                var task2 = Task.Factory.StartNew(() => threadIds.Add(Thread.CurrentThread.ManagedThreadId), CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler);

                tasksQueued.Set();

                Task.WaitAll(task1, task2);
                Assert.Equal(1, threadIds.Distinct().Count());
            }

            [Fact]
            public void RunAllTasksOnSamePartitionInOrder()
            {
                var tasks = new Task[200];
                var taskGroup1Order = new List<Int32>();
                var taskGroup2Order = new List<Int32>();
                var tasksQueued = new ManualResetEvent(false);
                var taskScheduler = new PartitionedTaskScheduler(task => task.AsyncState, 2, tasks.Length);

                for (var i = 0; i < 200; i++)
                {
                    var taskGroupOrder = i % 2 == 0 ? taskGroup1Order : taskGroup2Order;
                    var taskId = i;

                    tasks[taskId] = Task.Factory.StartNew(state =>
                        {
                            taskGroupOrder.Add(taskId);
                            tasksQueued.WaitOne();
                            Thread.Sleep(1);
                        }, taskId, CancellationToken.None, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning, taskScheduler);
                }

                tasksQueued.Set();

                Task.WaitAll(tasks);
                Assert.Equal(tasks.Length / 2, taskGroup1Order.Where((ordinal, index) => ordinal == (index * 2)).Count());
                Assert.Equal(tasks.Length / 2, taskGroup2Order.Where((ordinal, index) => ordinal == (index * 2) + 1).Count());
            }

            [Fact]
            public void RunTasksOnDifferentPartitionsInParallel()
            {
                var tasks = new Task[200];
                var taskOrder = new List<Int32>();
                var tasksQueued = new ManualResetEvent(false);
                var taskScheduler = new PartitionedTaskScheduler(task => task.AsyncState, 2, tasks.Length);

                for (var i = 0; i < 200; i++)
                {
                    var taskId = i;

                    tasks[taskId] = Task.Factory.StartNew(state =>
                        {
                            taskOrder.Add(taskId);
                            tasksQueued.WaitOne();
                            Thread.Sleep(taskId % 3);
                        }, taskId, CancellationToken.None, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning, taskScheduler);
                }

                tasksQueued.Set();

                Task.WaitAll(tasks);
                Assert.NotEqual(tasks.Length, taskOrder.Where((ordinal, index) => ordinal == index).Count());
            }

            [Fact]
            public void WillDeadlockIfForceSynchronousExecutionsAcrossPartitions()
            {
                var tasksQueued = new ManualResetEvent(false);
                var task1 = new Task(_ => { }, 1, CancellationToken.None, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning);
                var task2 = new Task(_ => { }, 2, CancellationToken.None, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning);
                var taskScheduler = new PartitionedTaskScheduler(task => task.AsyncState, 2, 4);

                var task3 = Task.Factory.StartNew(_ => { tasksQueued.WaitOne(); task1.RunSynchronously(); }, 2, CancellationToken.None, TaskCreationOptions.LongRunning, taskScheduler);
                var task4 = Task.Factory.StartNew(_ => { tasksQueued.WaitOne(); task2.RunSynchronously(); }, 1, CancellationToken.None, TaskCreationOptions.LongRunning, taskScheduler);

                tasksQueued.Set();

                Assert.False(Task.WaitAll(new[] { task1, task2, task3, task4 }, 100));
            }
        }

        public class WhenRunningTaskSynchronously
        {
            [Fact]
            public void AllowsInlineExecutionAfterBeingQueued()
            {
                var executions = 0;
                var taskScheduler = new PartitionedTaskScheduler();
                var task = new Task(() => { executions++; }, CancellationToken.None, TaskCreationOptions.AttachedToParent);

                task.RunSynchronously(taskScheduler);

                Assert.Equal(1, executions);
                Assert.Equal(0, taskScheduler.ScheduledTasks.Count());
            }

            [Fact]
            public void WaitForPreceedingTasksIfRequired()
            {
                var executionOrder = new List<Int32>();
                var tasksQueued = new ManualResetEvent(false);
                var taskScheduler = new PartitionedTaskScheduler();

                Task.Factory.StartNew(() => { executionOrder.Add(0); tasksQueued.WaitOne(); Thread.Sleep(25); }, CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler);
                Task.Factory.StartNew(() => executionOrder.Add(1), CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler);

                var synchronousTask = new Task(() => executionOrder.Add(2), CancellationToken.None, TaskCreationOptions.AttachedToParent);

                tasksQueued.Set();
                synchronousTask.RunSynchronously(taskScheduler);

                Assert.Equal(3, executionOrder.Where((value, index) => value == index).Count());
            }
        }
    }
}
