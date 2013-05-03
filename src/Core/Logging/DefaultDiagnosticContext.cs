using Spark.Infrastructure.Resources;
using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

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
    /// Diagnostic context without trace events for transfer, start and stop of logical operations.
    /// </summary>
    internal class DefaultDiagnosticContext : IDisposable
    {
        protected readonly TraceSource TraceSource;
        private readonly Guid originalActivityId;
        private readonly Guid currentActivityId;
        private readonly String name;
        private Boolean disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultDiagnosticContext"/>.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="name">The name</param>
        /// <param name="data">The context value(s)</param>
        /// <param name="activityId">The activity id.</param>
        /// <remarks><paramref name="data"/> can be <value>null</value>, an individual <see cref="Object"/> or an array of <see cref="Object"/>.</remarks>
        public DefaultDiagnosticContext(TraceSource traceSource, String name, Object data, Guid activityId)
        {
            this.name = GetContextName(name, data);
            this.originalActivityId = Trace.CorrelationManager.ActivityId;
            this.currentActivityId = activityId == Guid.Empty ? originalActivityId : activityId;

            TraceSource = traceSource;
            Transfer(originalActivityId, currentActivityId);
            StartLogicalOperation();
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="DefaultDiagnosticContext"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="DefaultDiagnosticContext"/> class.
        /// </summary>
        protected virtual void Dispose(Boolean disposing)
        {
            if (!disposing || disposed)
                return;

            if (!name.Equals(Trace.CorrelationManager.LogicalOperationStack.Peek()))
                throw new InvalidOperationException(Exceptions.OperationIdModifiedInsideScope);

            if (Trace.CorrelationManager.ActivityId != currentActivityId)
                throw new InvalidOperationException(Exceptions.ActivityIdModifiedInsideScope);

            StopLogicalOperation();
            Transfer(currentActivityId, originalActivityId);

            disposed = true;
        }

        /// <summary>
        /// Get the formatted context name.
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="data">The context value(s)</param>
        /// <remarks>
        /// Ideally would have been lazily processed to avoid cost unless message was actually logged given StartLogicalOperation should accept
        /// any object, however xUnit test runner ran in to issues as DiagnosticContext was did not implement ISerializable, which suggests other
        /// consumers may also have issues. Good candidate for future optimization if required (represents about 50% of the context creation cost).
        /// </remarks>
        private static String GetContextName(String name, Object data)
        {
            var sb = new StringBuilder();

            sb.Append("{ ");
            sb.Append(name);

            if (data != null)
            {
                var enumerable = data as IEnumerable;
                if (enumerable == null)
                {
                    sb.Append(", ");
                    sb.Append(data);
                }
                else
                {
                    foreach (var value in enumerable)
                    {
                        sb.Append(", ");
                        sb.Append(value);
                    }
                }
            }

            sb.Append(" }");

            return sb.ToString();
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

            OnTransfer(@from, to);
            Trace.CorrelationManager.ActivityId = to;
        }

        /// <summary>
        /// The <see cref="Transfer"/> extension point for derived classes.
        /// </summary>
        /// <param name="from">The original/current activity id.</param>
        /// <param name="to">The new/target activity id.</param>
        protected virtual void OnTransfer(Guid from, Guid to)
        { }

        /// <summary>
        /// Start a new logical operation.
        /// </summary>
        protected void StartLogicalOperation()
        {
            Trace.CorrelationManager.StartLogicalOperation(name);
            OnStartLogicalOperation();
        }

        /// <summary>
        /// The <see cref="StartLogicalOperation"/> extension point for derived classes.
        /// </summary>
        protected virtual void OnStartLogicalOperation()
        { }

        /// <summary>
        /// Stop an existing logical operation.
        /// </summary>
        protected void StopLogicalOperation()
        {
            OnStopLogicalOperation();
            Trace.CorrelationManager.StopLogicalOperation();
        }

        /// <summary>
        /// The <see cref="StopLogicalOperation"/> extension point for derived classes.
        /// </summary>
        protected virtual void OnStopLogicalOperation()
        { }

        /// <summary>
        /// Returns a string that represents the current diagnostic context.
        /// </summary>
        public override string ToString()
        {
            return name;
        }
    }
}
