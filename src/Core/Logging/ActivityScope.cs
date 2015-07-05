using System;
using System.Diagnostics;
using Spark.Resources;

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

namespace Spark.Logging
{
    /// <summary>
    /// Diagnostic context without trace events for transfer, start and stop of logical operations.
    /// </summary>
    internal sealed class ActivityScope : IDisposable
    {
        private readonly TraceSource traceSource;
        private readonly Guid originalActivityId;
        private readonly Guid currentActivityId;
        private readonly Boolean logTransfer;
        private Boolean disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="ActivityScope"/>.
        /// </summary>
        /// <param name="source">The trace source.</param>
        /// <param name="activityId">The activity id.</param>
        public ActivityScope(TraceSource source, Guid activityId)
            : this(source, activityId, true)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ActivityScope"/>.
        /// </summary>
        /// <param name="source">The trace source.</param>
        /// <param name="activityId">The activity id.</param>
        /// <param name="traceEnabled">Specify <value>true</value> to log <see cref="TraceEventType.Transfer"/> events; otherwise <value>false</value>.</param>
        public ActivityScope(TraceSource source, Guid activityId, Boolean traceEnabled)
        {
            traceSource = source;
            currentActivityId = activityId;
            originalActivityId = Trace.CorrelationManager.ActivityId;
            logTransfer = traceEnabled;

            Transfer(originalActivityId, currentActivityId);
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="ActivityScope"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            if (Trace.CorrelationManager.ActivityId != currentActivityId)
                throw new InvalidOperationException(Exceptions.ActivityIdModifiedInsideScope);

            Transfer(currentActivityId, originalActivityId);

            disposed = true;
        }

        /// <summary>
        /// Changes the <see cref="CorrelationManager"/>'s activity id to <paramref name="to"/>.
        /// </summary>
        /// <param name="from">The original/current activity id.</param>
        /// <param name="to">The new/target activity id.</param>
        private void Transfer(Guid from, Guid to)
        {
            if (from == to)
                return;

            if (logTransfer)
                traceSource.TraceTransfer(0, Messages.LogicalOperationTransfered.FormatWith(from, to), to);

            Trace.CorrelationManager.ActivityId = to;
        }

        /// <summary>
        /// Returns a string that represents the current diagnostic context.
        /// </summary>
        public override String ToString()
        {
            return currentActivityId.ToString();
        }
    }
}
