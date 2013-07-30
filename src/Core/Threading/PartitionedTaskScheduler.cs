using Spark.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

namespace Spark.Threading
{
    /// <summary>
    /// An ordered limited-concurrency task scheduler that ensures a given task partition is only active on a single 
    /// thread at a given time. This task scheduler is intended to maximize concurrent processing while ensuring that a
    /// given object (if hashed properly) is only processed by a single thread at a time. This task scheduler will also
    /// limit the maximum number of queued tasks if desired to ensure the thread pool is left available for other work.
    /// </summary>
    /// <example>
    /// Sample Hash Functions:
    /// <code>
    ///     // Bad Hash Function
    ///     new PartitionedTaskScheduler(task => task.Id.GetHashCode());
    /// 
    ///     // Better Hash Function
    ///     new PartitionedTaskScheduler(task => ((MyIdentifiableObject)task.AsyncState).Id);
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
        private static readonly Int32 MaximumWorkerThreads;
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<Int32, Partition> partitions = new Dictionary<Int32, Partition>();
        private readonly ISynchronizeAccess monitor;
        private readonly IQueueUserWorkItems threadPool;
        private readonly Func<Task, Int32> partitionHash;
        private readonly Int32 maximumConcurrencyLevel;
        private readonly Int32 boundedCapacity;
        private Int32 queuedTasks;

        /// <summary>
        /// Gets the bounded capacity of this <see cref="BlockingThreadPoolTaskScheduler"/> instance.
        /// </summary>
        public Int32 BoundedCapacity { get { return boundedCapacity; } }

        /// <summary>
        /// Indicates the maximum concurrency level this <see cref="TaskScheduler"/> is able to support.
        /// </summary>
        public override Int32 MaximumConcurrencyLevel { get { return maximumConcurrencyLevel; } }

        /// <summary>
        /// For debugger support only, generates an enumerable of <see cref="Task"/> instances currently queued to the scheduler waiting to be executed.
        /// </summary>
        internal IEnumerable<Task> ScheduledTasks { get { return GetScheduledTasks(); } }

        /// <summary>
        /// Initializes all static read-only members of <see cref="PartitionedTaskScheduler"/>.
        /// </summary>
        /// <remarks>Subsequent changes to the <see cref="ThreadPool"/>'s maximum worker threads will not im</remarks>
        static PartitionedTaskScheduler()
        {
            Int32 workerThreads, completionPortThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);

            MaximumWorkerThreads = workerThreads;
        }

        /// <summary>
        /// Initializes a deafult instance of <see cref="PartitionedTaskScheduler"/> with <see cref="BoundedCapacity"/> and <see cref="MaximumConcurrencyLevel"/> based on the maximum 
        /// number of thread thread pool worker threads.
        /// </summary>
        public PartitionedTaskScheduler()
            : this(_ => 0)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PartitionedTaskScheduler"/> with <see cref="BoundedCapacity"/> and <see cref="MaximumConcurrencyLevel"/> based on the maximum 
        /// number of thread thread pool worker threads.
        /// </summary>
        /// <param name="hash">The <see cref="Task"/> hash function used to determine the executor partition.</param>
        public PartitionedTaskScheduler(Func<Task, Object> hash)
            : this(hash, MaximumWorkerThreads)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PartitionedTaskScheduler"/> with the specified maximum concurrency level of <see cref="maximumConcurrencyLevel"/> 
        /// and <see cref="BoundedCapacity"/> based on the maximum number of thread thread pool worker threads.
        /// </summary>
        /// <param name="hash">The <see cref="Task"/> hash function used to determine the executor partition.</param>
        /// <param name="maximumConcurrencyLevel">The maximum number of concurrently executing tasks.</param>
        public PartitionedTaskScheduler(Func<Task, Object> hash, Int32 maximumConcurrencyLevel)
            : this(hash, maximumConcurrencyLevel, maximumConcurrencyLevel)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PartitionedTaskScheduler"/> with the specified maximum concurrency level of <see cref="maximumConcurrencyLevel"/> 
        /// and a bounded capacity of <see cref="boundedCapacity"/>.
        /// </summary>
        /// <param name="hash">The <see cref="Task"/> hash function used to determine the executor partition.</param>
        /// <param name="maximumConcurrencyLevel">The maximum number of concurrently executing tasks.</param>
        /// <param name="boundedCapacity">The bounded size of the task queue.</param>
        public PartitionedTaskScheduler(Func<Task, Object> hash, Int32 maximumConcurrencyLevel, Int32 boundedCapacity)
            : this(hash, maximumConcurrencyLevel, boundedCapacity, ThreadPoolWrapper.Instance, MonitorWrapper.Instance)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PartitionedTaskScheduler"/> with the specified maximum concurrency level of <see cref="maximumConcurrencyLevel"/> 
        /// and a bounded capacity of <see cref="boundedCapacity"/>.
        /// </summary>
        /// <param name="hash">The <see cref="Task"/> hash function used to determine the executor partition.</param>
        /// <param name="maximumConcurrencyLevel">The maximum number of concurrently executing tasks.</param>
        /// <param name="boundedCapacity">The bounded size of the task queue.</param>
        /// <param name="threadPool">The thread pool implementation on which to schedule tasks.</param>
        /// <param name="monitor">The monitor implementation used to synchronize object access.</param>
        internal PartitionedTaskScheduler(Func<Task, Object> hash, Int32 maximumConcurrencyLevel, Int32 boundedCapacity, IQueueUserWorkItems threadPool, ISynchronizeAccess monitor)
        {
            Verify.NotNull(hash, "hash");
            Verify.NotNull(monitor, "monitor");
            Verify.NotNull(threadPool, "threadPool");
            Verify.GreaterThan(0, boundedCapacity, "boundedCapacity");
            Verify.GreaterThan(0, maximumConcurrencyLevel, "maximumConcurrencyLevel");
            Verify.LessThanOrEqual(MaximumWorkerThreads, maximumConcurrencyLevel, "maximumConcurrencyLevel");

            Log.TraceFormat("BoundedCapacity={0}, MaximumConcurrencyLevel={1}", boundedCapacity, maximumConcurrencyLevel);

            this.monitor = monitor;
            this.threadPool = threadPool;
            this.boundedCapacity = boundedCapacity;
            this.maximumConcurrencyLevel = maximumConcurrencyLevel;
            this.partitionHash = task => Math.Abs((hash(task) ?? 0).GetHashCode()) % maximumConcurrencyLevel;
        }

        /// <summary>
        /// Queues a <see cref="Task"/> to the scheduler.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to be queued.</param>
        protected override void QueueTask(Task task)
        {
            using (Log.PushContext("Task", task.Id))
            {
                WaitIfRequired();

                var partition = GetOrCreatePartition(task);
                if (partition.Enqueue(task) != 1)
                {
                    Log.Trace("Task executor already scheduled");
                    return;
                }

                if ((task.CreationOptions & TaskCreationOptions.LongRunning) == TaskCreationOptions.LongRunning)
                {
                    var longRunningThread = new Thread(state => ExecutePartitionedTasks((Partition)state)) { IsBackground = true };

                    Log.Trace("Scheduling task executor on long running thread");

                    longRunningThread.Start(partition);
                }
                else
                {
                    Log.Trace("Scheduling task executor on thread pool");

                    threadPool.UnsafeQueueUserWorkItem(ExecutePartitionedTasks, partition);
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
            var partition = default(Partition);

            Log.TraceFormat("Getting partition {0}", partitionId);

            if (!partitions.TryGetValue(partitionId, out partition))
            {
                Log.Trace("Acquiring lock: partitions");
                lock (partitions)
                {
                    Log.Trace("Lock acquired: partitions");

                    if (!partitions.TryGetValue(partitionId, out partition))
                    {
                        Log.TraceFormat("Creating partition {0}", partitionId);

                        partitions.Add(partitionId, partition = new Partition(partitionId));
                    }

                    Log.Trace("Releasing lock: partitions");
                }
            }

            Log.TraceFormat("Task {0} mapped to partition {1}", task.Id, partitionId);

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
                Log.TraceFormat("Acquiring lock: partition {0}", partition.Id);
                lock (partition)
                {
                    Task task;

                    Log.TraceFormat("Lock acquired: partition {0}", partition.Id);

                    if (!partition.TryDequeue(out task))
                    {
                        Log.Trace("All queued tasks executed");
                        break;
                    }

                    ExecuteTask(task);

                    Log.TraceFormat("Releasing lock:  partition {0}", partition.Id);
                }

                Pulse(1);
            }
        }

        /// <summary>
        /// If the set of currently queued tasks exceeds <see cref="boundedCapacity"/> wait for a slot to be freed before proceeding; otherwise exit immediately.
        /// </summary>
        private void WaitIfRequired()
        {
            Log.Trace("Acquiring lock: partitions");
            lock (partitions)
            {
                Log.Trace("Lock acquired: partitions");

                while (queuedTasks >= boundedCapacity)
                {
                    Log.Trace("Maximum number of queued tasks reached; waiting for pulse");
                    monitor.Wait(partitions);
                }

                Log.Trace("Adding task to queue");
                queuedTasks++;

                Log.Trace("Releasing lock: partitions");
            }
        }

        /// <summary>
        /// Pulse <paramref name="count"/> threads that a <see cref="Task"/> execution slot has been made available.
        /// </summary>
        /// <param name="count">The number of threads to notify.</param>
        private void Pulse(Int32 count)
        {
            Log.Trace("Acquiring lock: partitions");
            lock (partitions)
            {
                Log.Trace("Lock acquired: partitions");

                for (var i = 0; i < count; i++)
                {
                    queuedTasks--;

                    Log.Trace("Pulsing");

                    monitor.Pulse(partitions);
                }

                Log.Trace("Releasing lock: partitions");
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

            Log.TraceFormat("Acquiring lock: partition {0}", partition.Id);
            lock (partition)
            {
                Log.TraceFormat("Lock acquired: partition {0}", partition.Id);

                Task queuedTask;
                while (partition.TryDequeue(out queuedTask) && !taskExecuted)
                {
                    ExecuteTask(queuedTask);
                    taskExecuted = queuedTask.Id == task.Id;
                    queuedTasksExecuted++;
                }

                if (!taskExecuted)
                    taskExecuted = ExecuteTask(task);

                Log.TraceFormat("Releasing lock: partition {0}", partition.Id);
            }

            Pulse(queuedTasksExecuted);

            return taskExecuted;
        }

        /// <summary>
        /// Attempts to execute the provided <see cref="Task"/> on this scheduler.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to be executed.</param>
        private Boolean ExecuteTask(Task task)
        {
            using (Log.PushContext("Task", task.Id))
            {
                Log.Trace("Executing task");

                return TryExecuteTask(task);
            }
        }

        /// <summary>
        /// For debugger support only, generates an enumerable of <see cref="Task"/> instances currently queued to the scheduler waiting to be executed.
        /// </summary>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            Task[] result;

            Log.Trace("Acquiring lock: partitions");
            lock (partitions)
            {
                Log.Trace("Lock acquired: partitions");

                result = partitions.ToArray().SelectMany(kvp => kvp.Value.Tasks).ToArray();

                Log.Trace("Releasing lock: partitions");
            }

            return result;
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
            /// Initializes a new instance of <see cref="Partition"/> identified by <paramref name="id"/>.
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
