using System;
using System.Diagnostics;
using Spark.Resources;

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

namespace Spark.Logging
{
    /// <summary>
    /// Diagnostic context without trace events for transfer, start and stop of logical operations.
    /// </summary>
    internal sealed class LogicalOperationScope : IDisposable
    {
        private readonly TraceSource traceSource;
        private readonly String operationId;
        private readonly Boolean logEvents;
        private Boolean disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="LogicalOperationScope"/>.
        /// </summary>
        /// <param name="source">The trace source.</param>
        /// <param name="name">The name</param>
        public LogicalOperationScope(TraceSource source, String name)
            : this(source, name, true)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="LogicalOperationScope"/>.
        /// </summary>
        /// <param name="source">The trace source.</param>
        /// <param name="name">The name</param>
        /// <param name="traceEnabled">Specify <value>true</value> to log <see cref="TraceEventType.Transfer"/> events; otherwise <value>false</value>.</param>
        public LogicalOperationScope(TraceSource source, String name, Boolean traceEnabled)
        {
            logEvents = traceEnabled;
            traceSource = source;
            operationId = name;

            StartLogicalOperation();
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="LogicalOperationScope"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            if (!operationId.Equals(Trace.CorrelationManager.LogicalOperationStack.Peek()))
                throw new InvalidOperationException(Exceptions.OperationIdModifiedInsideScope);

            StopLogicalOperation();
            disposed = true;
        }

        /// <summary>
        /// Start a new logical operation.
        /// </summary>
        private void StartLogicalOperation()
        {
            Trace.CorrelationManager.StartLogicalOperation(operationId);

            if (logEvents)
                traceSource.TraceEvent(TraceEventType.Start, 0, Messages.LogicalOperationStarted.FormatWith(operationId));
        }

        /// <summary>
        /// Stop an existing logical operation.
        /// </summary>
        private void StopLogicalOperation()
        {
            if (logEvents)
                traceSource.TraceEvent(TraceEventType.Stop, 0, Messages.LogicalOperationStopped.FormatWith(operationId));

            Trace.CorrelationManager.StopLogicalOperation();
        }

        /// <summary>
        /// Returns a string that represents the current diagnostic context.
        /// </summary>
        public override String ToString()
        {
            return operationId;
        }
    }
}
