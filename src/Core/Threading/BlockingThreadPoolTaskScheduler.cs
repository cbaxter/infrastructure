using Spark.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    /// Blocking <see cref="TaskScheduler"/> if number of queued tasks exceeds <see cref="BoundedCapacity"/>.
    /// </summary>
    public sealed class BlockingThreadPoolTaskScheduler : TaskScheduler
    {
        private static readonly Int32 MaximumWorkerThreads;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IDictionary<Int32, Task> queuedTasks = new Dictionary<Int32, Task>();
        private readonly IQueueUserWorkItems threadPool;
        private readonly ISynchronizeAccess monitor;
        private readonly Int32 boundedCapacity;

        /// <summary>
        /// Gets the bounded capacity of this <see cref="BlockingThreadPoolTaskScheduler"/> instance.
        /// </summary>
        public Int32 BoundedCapacity { get { return boundedCapacity; } }

        /// <summary>
        /// Indicates the maximum concurrency level this <see cref="TaskScheduler"/> is able to support.
        /// </summary>
        public override Int32 MaximumConcurrencyLevel { get { return MaximumWorkerThreads; } }

        /// <summary>
        /// For debugger support only, generates an enumerable of <see cref="Task"/> instances currently queued to the scheduler waiting to be executed.
        /// </summary>
        internal IEnumerable<Task> ScheduledTasks { get { return GetScheduledTasks(); } }

        /// <summary>
        /// Initializes all static read-only members of <see cref="BlockingThreadPoolTaskScheduler"/>.
        /// </summary>
        static BlockingThreadPoolTaskScheduler()
        {
            Int32 workerThreads, completionPortThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

            MaximumWorkerThreads = workerThreads;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BlockingThreadPoolTaskScheduler"/> using <see cref="MaximumConcurrencyLevel"/> as the bounded capacity.
        /// </summary>
        public BlockingThreadPoolTaskScheduler()
            : this(MaximumWorkerThreads)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlockingThreadPoolTaskScheduler"/> using <paramref name="boundedCapacity"/> as the bounded capacity.
        /// </summary>
        /// <param name="boundedCapacity">The bounded size of the task queue.</param>
        public BlockingThreadPoolTaskScheduler(Int32 boundedCapacity)
            : this(boundedCapacity, ThreadPoolWrapper.Instance, MonitorWrapper.Instance)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlockingThreadPoolTaskScheduler"/> using <paramref name="boundedCapacity"/> as the bounded capacity with an overriden thread pool and monitor class.
        /// </summary>
        /// <param name="boundedCapacity">The bounded size of the task queue.</param>
        /// <param name="threadPool">The thread pool implementation on which to schedule tasks.</param>
        /// <param name="monitor">The monitor implementation used to synchronize object access.</param>
        internal BlockingThreadPoolTaskScheduler(Int32 boundedCapacity, IQueueUserWorkItems threadPool, ISynchronizeAccess monitor)
        {
            Verify.GreaterThan(0, boundedCapacity, "boundedCapacity");
            Verify.NotNull(threadPool, "threadPool");

            Log.TraceFormat("BoundedCapacity={0}, MaximumConcurrencyLevel={1}", boundedCapacity, MaximumConcurrencyLevel);

            this.monitor = monitor;
            this.threadPool = threadPool;
            this.boundedCapacity = boundedCapacity;
        }

        /// <summary>
        /// Queues a <see cref="Task"/> to the scheduler.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to be queued.</param>
        protected override void QueueTask(Task task)
        {
            using (Log.PushContext("Task", task.Id))
            {
                WaitIfRequired(task);

                if ((task.CreationOptions & TaskCreationOptions.LongRunning) == TaskCreationOptions.LongRunning)
                {
                    var longRunningThread = new Thread(state => TryExecuteTaskInline((Task)state, true)) { IsBackground = true };

                    Log.Trace("Scheduling task executor on long running thread");

                    longRunningThread.Start(task);
                }
                else
                {
                    Log.Trace("Scheduling task executor on thread pool");

                    threadPool.UnsafeQueueUserWorkItem(state => TryExecuteTaskInline(state, true), task);
                }
            }
        }

        /// <summary>
        /// If the set of currently queued tasks exceeds <see cref="boundedCapacity"/> wait for a slot to be freed before proceeding; otherwise proceed immediately.
        /// </summary>
        private void WaitIfRequired(Task task)
        {
            Log.Trace("Acquiring lock");
            lock (queuedTasks)
            {
                Log.Trace("Lock acquired");

                while (queuedTasks.Count >= boundedCapacity)
                {
                    Log.Trace("Maximum number of queued tasks reached; waiting for pulse");
                    monitor.Wait(queuedTasks);
                }

                Log.Trace("Adding task to queue");
                queuedTasks.Add(task.Id, task);

                Log.Trace("Releasing lock");
            }
        }

        /// <summary>
        /// Determines whether the provided <see cref="Task"/> can be executed synchronously in this call, and if it can, executes it.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">A <see cref="Boolean"/> denoting whether or not the task has previously been queued.</param>
        protected override Boolean TryExecuteTaskInline(Task task, Boolean taskWasPreviouslyQueued)
        {
            using (Log.PushContext("Task", task.Id))
            {
                PulseIfRequired(task, taskWasPreviouslyQueued);

                Log.Trace("Executing task");
                return TryExecuteTask(task);
            }
        }

        /// <summary>
        /// Notify a thread in the waiting queue that a <see cref="Task"/> execution slot has been made available.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">A <see cref="Boolean"/> denoting whether or not the task has previously been queued.</param>
        private void PulseIfRequired(Task task, Boolean taskWasPreviouslyQueued)
        {
            Log.Trace("Acquiring lock");
            lock (queuedTasks)
            {
                Log.Trace("Lock acquired");

                if (queuedTasks.Remove(task.Id))
                {
                    Log.Trace("Removed task from queue");
                    Log.Trace("Pusling");
                    monitor.Pulse(queuedTasks);
                }
                else
                {
                    Log.Trace(taskWasPreviouslyQueued ? "Task already removed from queue" : "Task not previously queued");
                }

                Log.Trace("Releasing lock");
            }
        }

        /// <summary>
        /// For debugger support only, generates an enumerable of <see cref="Task"/> instances currently queued to the scheduler waiting to be executed.
        /// </summary>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            Task[] tasks;

            Log.Trace("Acquiring lock");
            lock (queuedTasks)
            {
                Log.Trace("Lock acquired");

                tasks = queuedTasks.Values.ToArray();

                Log.Trace("Releasing lock");
            }

            return tasks;
        }
    }
}
