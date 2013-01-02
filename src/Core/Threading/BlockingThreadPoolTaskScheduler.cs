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
    public sealed class BlockingThreadPoolTaskScheduler : TaskScheduler
    {
        public const Int32 ConcurrencyLevelMultiplier = 3;
        public static readonly Int32 DefaultMaximumQueuedTasks;
        public static readonly Int32 MaximumWorkerThreads;

        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IDictionary<Int32, Task> queuedTasks = new Dictionary<Int32, Task>();
        private readonly Object syncLock = new Object();
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

        static BlockingThreadPoolTaskScheduler()
        {
            Int32 workerThreads, completionPortThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

            MaximumWorkerThreads = workerThreads;
            DefaultMaximumQueuedTasks = workerThreads * 5;
            Log.InfoFormat("MaximumConcurrencyLevel={0}, DefaultMaximumQueuedTasks={1}", MaximumWorkerThreads, DefaultMaximumQueuedTasks);
        }

        public BlockingThreadPoolTaskScheduler()
            : this(DefaultMaximumQueuedTasks)
        { }

        public BlockingThreadPoolTaskScheduler(Int32 boundedCapacity)
            : this(boundedCapacity, ThreadPoolWrapper.Instance, MonitorWrapper.Instance)
        { }

        internal BlockingThreadPoolTaskScheduler(Int32 boundedCapacity, IQueueUserWorkItems threadPool, ISynchronizeAccess monitor)
        {
            Verify.GreaterThan(0, boundedCapacity, "boundedCapacity");
            Verify.NotNull(threadPool, "threadPool");

            this.monitor = monitor;
            this.threadPool = threadPool;
            this.boundedCapacity = boundedCapacity;
        }

        protected override void QueueTask(Task task)
        {
            using (Log.PushContext("Task", task.Id))
            {
                Log.Trace("Acquiring lock");
                lock (syncLock)
                {
                    Log.Trace("Lock acquired");

                    while (queuedTasks.Count >= boundedCapacity)
                    {
                        Log.Trace("Maximum number of queued tasks reached; waiting for pulse");
                        monitor.Wait(syncLock);
                    }

                    Log.Trace("Adding task to queue");
                    queuedTasks.Add(task.Id, task);

                    Log.Trace("Releasing lock");
                }

                Log.Trace("Scheduling user work item on thread pool");
                threadPool.UnsafeQueueUserWorkItem(state => TryExecuteTaskInline(state, true), task);
            }
        }

        protected override Boolean TryExecuteTaskInline(Task task, Boolean taskWasPreviouslyQueued)
        {
            using (Log.PushContext("Task", task.Id))
            {
                Log.Trace("Acquiring lock");
                lock (syncLock)
                {
                    Log.Trace("Lock acquired");

                    if (queuedTasks.Remove(task.Id))
                    {
                        Log.Trace("Removed task from queue");
                        Log.Trace("Pusling");
                        monitor.Pulse(syncLock);
                    }
                    else
                    {
                        if (taskWasPreviouslyQueued)
                            Log.Trace("Task already removed from queue");
                        else
                            Log.Trace("Task not previously queued");
                    }

                    Log.Trace("Releasing lock");
                }

                Log.Trace("Executing task");
                return TryExecuteTask(task);
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            Task[] tasks;

            Log.Trace("Acquiring lock");
            lock (syncLock)
            {
                Log.Trace("Lock acquired");

                tasks = queuedTasks.Values.ToArray();

                Log.Trace("Releasing lock");
            }

            return tasks;
        }
    }
}
