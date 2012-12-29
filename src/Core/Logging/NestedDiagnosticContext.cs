using Spark.Infrastructure.Resources;
using System;
using System.Diagnostics;

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

namespace Spark.Infrastructure.Logging
{
    /// <summary>
    /// Nested diagnostics context for supporting trace event correlation in logs.
    /// </summary>
    internal sealed class NestedDiagnosticContext : IDisposable
    {
        private readonly TraceSource traceSource;
        private readonly Object currentOperationId;
        private readonly Guid originalActivityId;
        private readonly Guid currentActivityId;
        private readonly DateTime startTime;
        private volatile Boolean disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="NestedDiagnosticContext"/>.
        /// </summary>
        /// <param name="source">The underlying <see cref="TraceSource"/> that created this <see cref="NestedDiagnosticContext"/>.</param>
        /// <param name="activityId">The trace correlation id.</param>
        /// <param name="operationId">The logical operation id.</param>
        public NestedDiagnosticContext(TraceSource source, Guid activityId, Object operationId)
        {
            Verify.NotNull(source, "source");

            traceSource = source;
            currentOperationId = operationId ?? Guid.NewGuid();
            currentActivityId = activityId != Guid.Empty ? activityId : Guid.NewGuid();
            originalActivityId = Trace.CorrelationManager.ActivityId;

            // Set the current ActivityId if changed.
            if (originalActivityId != currentActivityId)
            {
                traceSource.TraceTransfer(0, Messages.LogicalOperationTransfered.FormatWith(originalActivityId, currentActivityId), currentActivityId);
                Trace.CorrelationManager.ActivityId = currentActivityId;
            }
            
            // Push the new logical operation on to the call context stack.
            startTime = DateTime.Now;
            Trace.CorrelationManager.StartLogicalOperation(currentOperationId);
            traceSource.TraceEvent(TraceEventType.Start, 0, Messages.LogicalOperationStarted.FormatWith(operationId));
        }
        
        /// <summary>
        /// Releases all unmanaged resources used by the current instance of the <see cref="NestedDiagnosticContext"/> class.
        /// </summary>
        ~NestedDiagnosticContext()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="NestedDiagnosticContext"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="NestedDiagnosticContext"/> class.
        /// </summary>
        public void Dispose(Boolean disposing)
        {
            if (disposing || disposed)
                return;

            if (Trace.CorrelationManager.LogicalOperationStack.Peek() != currentOperationId)
                throw new InvalidOperationException(Exceptions.OperationIdModifiedInsideScope);

            if (Trace.CorrelationManager.ActivityId != currentActivityId)
                throw new InvalidOperationException(Exceptions.ActivityIdModifiedInsideScope);

            // Pop the current logical operation from the call context stack.
            traceSource.TraceEvent(TraceEventType.Stop, 0, Messages.LogicalOperationStopped.FormatWith(currentOperationId, DateTime.Now.Subtract(startTime)));
            Trace.CorrelationManager.StopLogicalOperation();

            // Restore the original ActivityId if changed.
            if (originalActivityId != currentActivityId)
            {
                traceSource.TraceTransfer(0, Messages.LogicalOperationTransfered.FormatWith(currentActivityId, originalActivityId), originalActivityId);
                Trace.CorrelationManager.ActivityId = originalActivityId;
            }

            disposed = true;
        }
    }
}
