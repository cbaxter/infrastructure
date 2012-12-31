using Spark.Infrastructure.Logging;
using System;
using System.Collections.Concurrent;
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
    /// An ordered limited-concurrency task scheduler that ensures a given task partition is only active on a single 
    /// thread at a given time. This task scheduler is intended to maximize concurrent processing while ensuring that a
    /// given object (if hashed properly) is only processed by a single thread at a time. This task scheduler will also
    /// limit the maximium number of queued tasks if desired to ensure the thread pool is left available for other work.
    /// </summary>
    /// <example>
    /// Sample Hash Functions:
    /// <code>
    ///     // Bad Hash Function
    ///     new PartitionedTaskScheduler(task => task.Id.GetHashCode());
    /// 
    ///     // Better Hash Function
    ///     new PartitionedTaskScheduler(task => ((MyIdentifiableObject)task.AsyncState).Id.GetHashCode());
    /// </code>
    /// </example>
    /// <remarks>
    /// As task order is maintained, any tasks that use this task scheduler should ensure they do not force inline execution
    /// across threads (i.e., lock multiple partitions). This may result in a deadlock. Regular usage of queue task and forget
    /// will not cause any issues (that includes when attached to a parent task that is waited or cancelled). However, if within
    /// a given task you explicitly invoke RunSynchronously or Wait that forces inline execution of a task on a separate partition
    /// deadlock will likely occur and this isn't the right scheduler for you.
    /// </remarks>
    public sealed class PartitionedTaskScheduler : TaskScheduler
    {
        public const Int32 ConcurrencyLevelMultiplier = 3;
        public static readonly Int32 DefaultMaximumQueuedTasks;
        public static readonly Int32 DefaultMaximumConcurrencyLevel;

        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly ConcurrentDictionary<Int32, Partition> partitions = new ConcurrentDictionary<Int32, Partition>();
        private readonly Object syncLock = new Object();
        private readonly Func<Task, Int32> partitionHash;
        private readonly Int32 maximumConcurrencyLevel;
        private readonly Int32 maximumQueuedTasks;
        private Int32 queuedTasks;

        /// <summary>
        /// Indicates the maximum concurrency level this <see cref="TaskScheduler"/> is able to support.
        /// </summary>
        public override int MaximumConcurrencyLevel { get { return maximumConcurrencyLevel; } }

        /// <summary>
        /// Initializes all static read-only members of <see cref="PartitionedTaskScheduler"/>.
        /// </summary>
        /// <remarks>Subsequent changes to the <see cref="ThreadPool"/>'s maximum worker threads will not im</remarks>
        static PartitionedTaskScheduler()
        {
            Int32 workerThreads, completionPortThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

            DefaultMaximumConcurrencyLevel = workerThreads;
            DefaultMaximumQueuedTasks = DefaultMaximumConcurrencyLevel * ConcurrencyLevelMultiplier;
            Log.InfoFormat("DefaultMaximumConcurrencyLevel={0}, DefaultMaximumQueuedTasks={1}", DefaultMaximumConcurrencyLevel, DefaultMaximumQueuedTasks);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PartitionedTaskScheduler"/> with a maximum concurrency level of <see cref="DefaultMaximumConcurrencyLevel"/> 
        /// and <see cref="DefaultMaximumQueuedTasks"/> maximum queued tasks.
        /// </summary>
        /// <param name="hash">The <see cref="Task"/> hash function used to determine the executor partition.</param>
        public PartitionedTaskScheduler(Func<Task, Int32> hash)
            : this(hash, DefaultMaximumConcurrencyLevel, DefaultMaximumQueuedTasks)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PartitionedTaskScheduler"/> with a maximum concurrency level of <see cref="maximumConcurrencyLevel"/> 
        /// and <value><paramref name="maximumConcurrencyLevel"/> * <see cref="ConcurrencyLevelMultiplier"/></value> maximum queued tasks.
        /// </summary>
        /// <param name="hash">The <see cref="Task"/> hash function used to determine the executor partition.</param>
        /// <param name="maximumConcurrencyLevel">The maximum number of concurrently executing tasks.</param>
        public PartitionedTaskScheduler(Func<Task, Int32> hash, Int32 maximumConcurrencyLevel)
            : this(hash, maximumConcurrencyLevel, maximumConcurrencyLevel * ConcurrencyLevelMultiplier)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PartitionedTaskScheduler"/> with a maximum concurrency level of <paramref name="maximumConcurrencyLevel"/> 
        /// and <paramref name="maximumQueuedTasks"/> maximum queued tasks.
        /// </summary>
        /// <param name="hash">The <see cref="Task"/> hash function used to determine the executor partition.</param>
        /// <param name="maximumConcurrencyLevel">The maximum number of concurrently executing tasks.</param>
        /// <param name="maximumQueuedTasks">The maximum number of queued and/or executing tasks.</param>
        public PartitionedTaskScheduler(Func<Task, Int32> hash, Int32 maximumConcurrencyLevel, Int32 maximumQueuedTasks)
        {
            Verify.NotNull(hash, "hash");
            Verify.GreaterThan(0, maximumQueuedTasks, "maximumConcurrencyLevel");
            Verify.GreaterThan(0, maximumConcurrencyLevel, "maximumConcurrencyLevel");

            this.maximumQueuedTasks = maximumQueuedTasks;
            this.maximumConcurrencyLevel = maximumConcurrencyLevel;
            this.partitionHash = task => Math.Abs(hash(task)) % maximumConcurrencyLevel;
        }

        /// <summary>
        /// Queues a <see cref="Task"/> to the scheduler.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to be queued.</param>
        protected override void QueueTask(Task task)
        {
            var partition = GetOrCreatePartition(task);

            WaitIfRequired();
            using (Log.PushContext("Task", task.Id))
            {
                if (partition.Enqueue(task) != 1)
                    return;

                if ((task.CreationOptions & TaskCreationOptions.LongRunning) == TaskCreationOptions.LongRunning)
                {
                    var longRunningThread = new Thread(state => ExecutePartitionedTasks((Partition)state)) { IsBackground = true };

                    Log.Trace("Queuing new user work item on long running thread.");

                    longRunningThread.Start(partition);
                }
                else
                {
                    Log.Trace("Queuing new user work item on thread-pool.");

                    ThreadPool.UnsafeQueueUserWorkItem(state => ExecutePartitionedTasks((Partition)state), partition);
                }
            }
        }

        /// <summary>
        /// Get or create a partition based on the <paramref name="task"/> partition hash code.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to partition.</param>
        private Partition GetOrCreatePartition(Task task)
        {
            var partitionId = partitionHash(task);
            var partition = partitions.GetOrAdd(partitionId, id => new Partition(partitionId));

            Log.DebugFormat("Task {0} mapped to partition {1}.", task.Id, partition.Id);

            return partition;
        }

        /// <summary>
        /// Executes all queued <see cref="Task"/>'s associated with the specified <see cref="Partition"/>.
        /// </summary>
        /// <param name="partition">The <see cref="Partition"/> to process.</param>
        private void ExecutePartitionedTasks(Partition partition)
        {
            while (true)
            {
                lock (partition)
                {
                    Task task;

                    if (!partition.TryDequeue(out task))
                        break;

                    using (Log.PushContext("Task", task.Id))
                        TryExecuteTask(task);
                }

                Pulse(1);
            }
        }

        /// <summary>
        /// If the set of currently queued tasks exceeds <value>maximumQueuedTasks</value> wait for a slot to be freed before proceeding; otherwise exit immediately.
        /// </summary>
        private void WaitIfRequired()
        {
            lock (syncLock)
            {
                if (++queuedTasks > maximumQueuedTasks)
                {
                    Log.Trace("*** WAIT ***");
                    Monitor.Wait(syncLock);
                }
            }
        }

        /// <summary>
        /// Pulse <paramref name="count"/> threads that a <see cref="Task"/> execution slot has been made available.
        /// </summary>
        /// <param name="count">The number of threads to notify.</param>
        private void Pulse(Int32 count)
        {
            lock (syncLock)
            {
                Log.TraceFormat("*** PULSE {0} ***", count);

                for (var i = 0; i < count; i++)
                {
                    queuedTasks--;

                    Monitor.Pulse(syncLock);
                }
            }
        }

        /// <summary>
        /// Determines whether the provided <see cref="Task"/> can be executed synchronously in this call, and if it can, executes it.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">A <see cref="Boolean"/> denoting whether or not the task has previously been queued.</param>
        protected override Boolean TryExecuteTaskInline(Task task, Boolean taskWasPreviouslyQueued)
        {
            var taskExecuted = false;
            var queuedTasksExecuted = 0;
            var partition = GetOrCreatePartition(task);

            using (Log.PushContext("Task", task.Id))
            {
                lock (partition)
                {
                    Task queuedTask;
                    while (partition.TryDequeue(out queuedTask) && !taskExecuted)
                    {
                        queuedTasksExecuted++;
                        TryExecuteTask(queuedTask);
                        taskExecuted = queuedTask.Id == task.Id;
                    }

                    if (!taskExecuted)
                        TryExecuteTask(task);
                }
            }

            Pulse(queuedTasksExecuted);

            return true;
        }

        /// <summary>
        /// For debugger support only, generates an enumerable of <see cref="Task"/> instances currently queued to the scheduler waiting to be executed.
        /// </summary>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return partitions.ToArray().SelectMany(kvp => kvp.Value.Tasks);
        }

        /// <summary>
        /// Thread-safe <see cref="LinkedList{Task}"/> wrapper.
        /// </summary>
        /// <remarks>This type is safe for multithreaded read and write operations.</remarks>
        private sealed class Partition
        {
            private readonly LinkedList<Task> tasks = new LinkedList<Task>();
            private readonly Int32 id;

            /// <summary>
            /// The unique identifier for this <see cref="Partition"/> instance.
            /// </summary>
            public Int32 Id { get { return id; } }

            /// <summary>
            /// For debugger support only, generates an enumerable of <see cref="Task"/> instances currently queued to the scheduler waiting to be executed.
            /// </summary>
            public IEnumerable<Task> Tasks { get { lock (tasks) { return tasks.AsReadOnly(); } } }

            /// <summary>
            /// Intializes a new insance of <see cref="Partition"/> identified by <paramref name="id"/>.
            /// </summary>
            /// <param name="id">The unique identifier for this <see cref="Partition"/> instance.</param>
            public Partition(Int32 id)
            {
                this.id = id;
            }

            /// <summary>
            /// Adds the specified <paramref name="task"/> to the end of the queued <see cref="Partition"/> tasks.
            /// </summary>
            /// <param name="task">The <see cref="Task"/> to queue.</param>
            /// <returns>The current number of tasks queued in this <see cref="Partition"/>.</returns>
            public Int32 Enqueue(Task task)
            {
                lock (tasks)
                {
                    tasks.AddLast(task);

                    return tasks.Count;
                }
            }

            /// <summary>
            /// Attempts to dequeue the next <see cref="Task"/> queued in this <see cref="Partition"/>.
            /// </summary>
            /// <param name="task">The dequeued <see cref="Task"/> if exists; otherwise <value>null</value>.</param>
            /// <returns>True if a <see cref="Task"/> was dequeued; otherwise false.</returns>
            public Boolean TryDequeue(out Task task)
            {
                lock (tasks)
                {
                    Boolean taskDequeued;

                    if (tasks.First == null)
                    {
                        task = null;
                        taskDequeued = false;
                    }
                    else
                    {
                        task = tasks.First.Value;
                        tasks.RemoveFirst();
                        taskDequeued = true;
                    }

                    return taskDequeued;
                }
            }
        }
    }
}
