using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Spark.Logging;
using Spark.Resources;
using Xunit;

/* Copyright (c) 2014 Spark Software Ltd.
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

namespace Test.Spark.Logging
{
    public static class UsingActivityScope
    {
        public class WhenCreatingNewDiagnosticContext
        {
            [Fact]
            public void ActivityIdUnchangedIfSameGuidSpecified()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var activityId = Guid.NewGuid();

                Trace.CorrelationManager.ActivityId = activityId;
                using (new ActivityScope(traceSource, activityId))
                    Assert.Equal(activityId, Trace.CorrelationManager.ActivityId);
            }

            [Fact]
            public void ActivityIdChangedIfNewGuidSpecified()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var activityId = Guid.NewGuid();

                using (new ActivityScope(traceSource, activityId))
                    Assert.Equal(activityId, Trace.CorrelationManager.ActivityId);
            }

            [Fact]
            public void DoNotTraceTransferIfSameActivityId()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var listener = new FakeTraceListener();
                var activityId = Guid.NewGuid();

                traceSource.Listeners.Add(listener);

                Trace.CorrelationManager.ActivityId = activityId;
                using (new ActivityScope(traceSource, activityId))
                    Assert.Equal(0, listener.Messages.Count(m => m == String.Format("Transfer from {0} to {1}, relatedActivityId={1}", activityId, activityId)));
            }

            [Fact]
            public void DoNotTraceTransferIfTracingDisabled()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var listener = new FakeTraceListener();
                var activityId = Guid.NewGuid();

                traceSource.Listeners.Add(listener);

                Trace.CorrelationManager.ActivityId = activityId;
                using (new ActivityScope(traceSource, Guid.NewGuid(), traceEnabled: false))
                    Assert.Equal(0, listener.Messages.Count(m => m == String.Format("Transfer from {0} to {1}, relatedActivityId={1}", activityId, activityId)));
            }

            [Fact]
            public void TraceTransferIfNewActivityId()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var listener = new FakeTraceListener();
                var activityId = Guid.NewGuid();

                traceSource.Listeners.Add(listener);

                // NOTE: Although the default implementation of TraceListener.TraceTransfer will append `, relatedActivityId={1}`, the
                //       TraceListener.TraceTransfer can be overriden and thus appending `, relatedActivityId={1}` is not guaranteed. 
                //       Explicitly include the new activity id in the message even though the activity id may be repeated.

                Trace.CorrelationManager.ActivityId = Guid.Empty;
                using (new ActivityScope(traceSource, activityId))
                    Assert.Equal(1, listener.Messages.Count(m => m == String.Format("Transfer from {0} to {1}, relatedActivityId={1}", Guid.Empty, activityId)));
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnActivityId()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var activityId = Guid.NewGuid();

                using (var context = new ActivityScope(traceSource, activityId))
                    Assert.Equal(activityId.ToString(), context.ToString());
            }
        }

        // ReSharper disable AccessToDisposedClosure
        public class WhenDisposingExistingDiagnosticContext
        {
            [Fact]
            public void CanDisposeRepeatedly()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);

                using (var context = new ActivityScope(traceSource, Guid.Empty))
                    context.Dispose();
            }

            [Fact]
            public void CannotInterleaveActivityIds()
            {
                var activityId = Guid.NewGuid();
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var context = new ActivityScope(traceSource, activityId);

                Trace.CorrelationManager.ActivityId = Guid.NewGuid();

                var ex = Assert.Throws<InvalidOperationException>(() => context.Dispose());
                Assert.Equal(Exceptions.ActivityIdModifiedInsideScope, ex.Message);

                Trace.CorrelationManager.ActivityId = activityId;

                context.Dispose();
            }

            [Fact]
            public void FinalizerIgnoresContextInterleave()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var finalizer = typeof(ActivityScope).GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

                Trace.CorrelationManager.ActivityId = Guid.Empty;
                using (var context = new ActivityScope(traceSource, Guid.Empty))
                {
                    Trace.CorrelationManager.ActivityId = Guid.NewGuid();
                    finalizer.Invoke(context, null);
                    Trace.CorrelationManager.ActivityId = Guid.Empty;
                }
            }

            [Fact]
            public void DoNotTraceTransferIfSameActivityId()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var listener = new FakeTraceListener();
                var activityId = Guid.NewGuid();

                traceSource.Listeners.Add(listener);

                Trace.CorrelationManager.ActivityId = activityId;
                new ActivityScope(traceSource, activityId).Dispose();
                Assert.Equal(0, listener.Messages.Count(m => m == String.Format("Transfer from {0} to {1}, relatedActivityId={1}", activityId, activityId)));
            }

            [Fact]
            public void DoNotTraceTransferIfTracingDisabled()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var listener = new FakeTraceListener();
                var activityId = Guid.NewGuid();

                traceSource.Listeners.Add(listener);

                Trace.CorrelationManager.ActivityId = activityId;
                new ActivityScope(traceSource, Guid.NewGuid(), traceEnabled: false).Dispose();
                Assert.Equal(0, listener.Messages.Count(m => m == String.Format("Transfer from {0} to {1}, relatedActivityId={1}", activityId, activityId)));
            }

            [Fact]
            public void TraceTransferIfNewActivityId()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var listener = new FakeTraceListener();
                var activityId = Guid.NewGuid();

                traceSource.Listeners.Add(listener);

                // NOTE: Although the default implementation of TraceListener.TraceTransfer will append `, relatedActivityId={1}`, the
                //       TraceListener.TraceTransfer can be overriden and thus appending `, relatedActivityId={1}` is not guaranteed. 
                //       Explicitly include the new activity id in the message even though the activity id may be repeated.

                Trace.CorrelationManager.ActivityId = Guid.Empty;
                new ActivityScope(traceSource, activityId).Dispose();
                Assert.Equal(1, listener.Messages.Count(m => m == String.Format("Transfer from {0} to {1}, relatedActivityId={1}", Guid.Empty, activityId)));
            }
        }
        // ReSharper restore AccessToDisposedClosure
    }
}
