using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Spark.Threading
{
    /// <summary>
    /// Provides a task scheduler that runs tasks on the current thread immediately.
    /// </summary>
    public sealed class InlineTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// Default instance of <see cref="InlineTaskScheduler"/>.
        /// </summary>
        public static readonly InlineTaskScheduler Instance = new InlineTaskScheduler();

        /// <summary>
        /// Indicates the maximum concurrency level this <see cref="TaskScheduler"/> is able to support.
        /// </summary>
        public override int MaximumConcurrencyLevel { get { return 1; } }

        /// <summary>
        /// For debugger support only, generates an enumerable of <see cref="Task"/> instances currently queued to the scheduler waiting to be executed.
        /// </summary>
        internal IEnumerable<Task> ScheduledTasks { get { return GetScheduledTasks(); } }

        /// <summary>
        /// Attempts to execute the provided <see cref="Task"/> on this scheduler.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to be executed.</param>
        protected override void QueueTask(Task task)
        {
            TryExecuteTask(task);
        }

        /// <summary>
        /// Determines whether the provided <see cref="Task"/> can be executed synchronously in this call, and if it can, executes it.
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
