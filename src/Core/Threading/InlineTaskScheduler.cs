using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spark.Infrastructure.Threading
{
    /// <summary>
    /// Provides a task scheduler that runs tasks on the current thread immediately.
    /// </summary>
    public sealed class InlineTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// Indicates the maximum concurrency level this <see cref="TaskScheduler"/> is able to support.
        /// </summary>
        public override int MaximumConcurrencyLevel { get { return 1; } }

        /// <summary>
        /// Attempts to execute the provided <see cref="Task"/> on this scheduler.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to be executed.</param>
        protected override void QueueTask(Task task)
        {
            TryExecuteTask(task);
        }

        /// <summary>
        /// Determines whether the provided System.Threading.Tasks.Task can be executed synchronously in this call, and if it can, executes it.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">A <see cref="Boolean"/> denoting whether or not the task has previously been queued.</param>
        protected override Boolean TryExecuteTaskInline(Task task, Boolean taskWasPreviouslyQueued)
        {
            return TryExecuteTask(task);
        }

        /// <summary>
        /// For debugger support only, generates an enumerable of <see cref="Task"/> instances currently queued to the scheduler waiting to be executed.
        /// </summary>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return Enumerable.Empty<Task>();
        }
    }
}
