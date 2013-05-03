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
    /// Diagnostic context with trace events for transfer, start and stop of logical operations.
    /// </summary>
    internal sealed class DebugDiagnosticContext : DefaultDiagnosticContext
    {
        private readonly DateTime startTime;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultDiagnosticContext"/>.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="name">The name</param>
        /// <param name="context">The context value(s)</param>
        /// <param name="activityId">The activity id.</param>
        /// <remarks><paramref name="context"/> can be <value>null</value>, an individual <see cref="Object"/> or an array of <see cref="Object"/>.</remarks>
        public DebugDiagnosticContext(TraceSource traceSource, String name, Object context, Guid activityId)
            : base(traceSource, name, context, activityId)
        {
            this.startTime = DateTime.Now;
        }

        /// <summary>
        /// Extends the default transfer behavior with the logging of a <see cref="TraceEventType.Transfer"/> event.
        /// </summary>
        /// <param name="from">The original/current activity id.</param>
        /// <param name="to">The new/target activity id.</param>
        protected override void OnTransfer(Guid from, Guid to)
        {
            TraceSource.TraceTransfer(0, Messages.LogicalOperationTransfered.FormatWith(from, to), to);
        }

        /// <summary>
        /// Extends the default start logical operation behavior with the logging of a <see cref="TraceEventType.Start"/> event.
        /// </summary>
        protected override void OnStartLogicalOperation()
        {
            TraceSource.TraceEvent(TraceEventType.Start, 0, Messages.LogicalOperationStarted.FormatWith(this));
        }

        /// <summary>
        /// Extends the default stop logical operation behavior with the logging of a <see cref="TraceEventType.Stop"/> event.
        /// </summary>
        protected override void OnStopLogicalOperation()
        {
            TraceSource.TraceEvent(TraceEventType.Stop, 0, Messages.LogicalOperationStopped.FormatWith(this, DateTime.Now.Subtract(startTime)));
        }
    }
}
