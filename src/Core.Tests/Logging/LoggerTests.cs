using Spark.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;
using Xunit.Extensions;

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

namespace Spark.Tests.Logging
{
    public static class UsingLogger
    {
        public class WhenCreatingLogger
        {
            [Fact]
            public void FatalEnabledIfSourceLevelsCritical()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Critical);

                Assert.True(logger.IsFatalEnabled);
            }

            [Fact]
            public void ErrorEnabledIfSourceLevelsError()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Error);

                Assert.True(logger.IsErrorEnabled);
            }

            [Fact]
            public void WarnEnabledIfSourceLevelsWarning()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Warning);

                Assert.True(logger.IsWarnEnabled);
            }

            [Fact]
            public void InfoEnabledIfSourceLevelsInformation()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Information);

                Assert.True(logger.IsInfoEnabled);
            }

            [Fact]
            public void DebugEnabledIfSourceLevelsVerbose()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Verbose);

                Assert.True(logger.IsDebugEnabled);
            }

            [Fact]
            public void TraceEnabledIfSourceLevelsAll()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.All);

                Assert.True(logger.IsTraceEnabled);
            }

            [Theory, InlineData(SourceLevels.Critical), InlineData(SourceLevels.Error), InlineData(SourceLevels.Warning), InlineData(SourceLevels.Information)]
            public void DisableDiagnosticContextIfSourceLevels(SourceLevels level)
            {
                var logger = new Logger("MyTestLogger", level);

                Trace.CorrelationManager.ActivityId = Guid.Empty;
                using (logger.PushContext(Guid.NewGuid(), MethodBase.GetCurrentMethod().Name))
                    Assert.Equal(Guid.Empty, Trace.CorrelationManager.ActivityId);
            }

            [Theory, InlineData(SourceLevels.Verbose), InlineData(SourceLevels.All), InlineData(SourceLevels.ActivityTracing)]
            public void EnableDiagnosticContextIfSourceLevels(SourceLevels level)
            {
                var logger = new Logger("MyTestLogger", level);
                var activityId = Guid.NewGuid();

                Trace.CorrelationManager.ActivityId = Guid.Empty;
                using (logger.PushContext(activityId, MethodBase.GetCurrentMethod().Name))
                    Assert.Equal(activityId, Trace.CorrelationManager.ActivityId);
            }

            [Theory, InlineData(SourceLevels.Critical), InlineData(SourceLevels.Error), InlineData(SourceLevels.Warning), InlineData(SourceLevels.Information), InlineData(SourceLevels.Verbose), InlineData(SourceLevels.All), InlineData(SourceLevels.ActivityTracing)]
            public void DoNotTraceDiagnosticContextIfSourceLevels(SourceLevels level)
            {
                var logger = new Logger("MyTestLogger", level);
                var listener = new FakeTraceListener();
                var activityId = Guid.NewGuid();

                Trace.CorrelationManager.ActivityId = Guid.Empty;
                using (logger.PushContext(activityId, MethodBase.GetCurrentMethod().Name))
                    Assert.Equal(0, listener.Messages.Count());
            }

            [Theory, InlineData(SourceLevels.All)]
            public void TraceDiagnosticContextIfSourceLevels(SourceLevels level)
            {
                var logger = new Logger("MyTestLogger", level);
                var listener = new FakeTraceListener();
                var activityId = Guid.NewGuid();

                logger.TraceSource.Listeners.Add(listener);

                Trace.CorrelationManager.ActivityId = Guid.Empty;
                using (logger.PushContext(activityId, MethodBase.GetCurrentMethod().Name))
                    Assert.Equal(1, listener.Messages.Count(m => m.StartsWith(String.Format("Transfer from {0} to {1}", Guid.Empty, activityId))));
            }
        }

        public class WhenLoggingFatal
        {
            private static Expression<Action<ILog>> CreateExpressionFor(Expression<Action<ILog>> expression)
            {
                return expression;
            }

            public static IEnumerable<Object[]> FatalMethods
            {
                get
                {
                    yield return new Object[] { CreateExpressionFor(log => log.Fatal(new Exception())), new Exception().ToString() };
                    yield return new Object[] { CreateExpressionFor(log => log.Fatal("My Test Message")), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.FatalFormat("My Test Message {0}", 0)), "My Test Message 0" };
                    yield return new Object[] { CreateExpressionFor(log => log.FatalFormat("My Test Message {0} {1}", 0, 1)), "My Test Message 0 1" };
                    yield return new Object[] { CreateExpressionFor(log => log.FatalFormat("My Test Message {0} {1} {2}", 0, 1, 2)), "My Test Message 0 1 2" };
                    yield return new Object[] { CreateExpressionFor(log => log.FatalFormat("My Test Message {0} {1} {2} {3}", 0, 1, 2, 3)), "My Test Message 0 1 2 3" };
                    yield return new Object[] { CreateExpressionFor(log => log.Fatal(m => m("My Test Message"))), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.Fatal(() => "My Test Message")), "My Test Message" };
                }
            }

            [Theory, PropertyData("FatalMethods")]
            public void LogMessageIfFatalEnabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Critical);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(1, listener.Messages.Count(m => m == expectedMessage));
            }

            [Theory, PropertyData("FatalMethods")]
            public void DoNotLogMessageIfFatalDisabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(0, listener.Messages.Count(m => m == expectedMessage));
            }
        }

        public class WhenLoggingError
        {
            private static Expression<Action<ILog>> CreateExpressionFor(Expression<Action<ILog>> expression)
            {
                return expression;
            }

            public static IEnumerable<Object[]> ErrorMethods
            {
                get
                {
                    yield return new Object[] { CreateExpressionFor(log => log.Error(new Exception())), new Exception().ToString() };
                    yield return new Object[] { CreateExpressionFor(log => log.Error("My Test Message")), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.ErrorFormat("My Test Message {0}", 0)), "My Test Message 0" };
                    yield return new Object[] { CreateExpressionFor(log => log.ErrorFormat("My Test Message {0} {1}", 0, 1)), "My Test Message 0 1" };
                    yield return new Object[] { CreateExpressionFor(log => log.ErrorFormat("My Test Message {0} {1} {2}", 0, 1, 2)), "My Test Message 0 1 2" };
                    yield return new Object[] { CreateExpressionFor(log => log.ErrorFormat("My Test Message {0} {1} {2} {3}", 0, 1, 2, 3)), "My Test Message 0 1 2 3" };
                    yield return new Object[] { CreateExpressionFor(log => log.Error(m => m("My Test Message"))), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.Error(() => "My Test Message")), "My Test Message" };
                }
            }

            [Theory, PropertyData("ErrorMethods")]
            public void LogMessageIfErrorEnabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Error);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(1, listener.Messages.Count(m => m == expectedMessage));
            }

            [Theory, PropertyData("ErrorMethods")]
            public void DoNotLogMessageIfErrorDisabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(0, listener.Messages.Count(m => m == expectedMessage));
            }
        }

        public class WhenLoggingWarn
        {
            private static Expression<Action<ILog>> CreateExpressionFor(Expression<Action<ILog>> expression)
            {
                return expression;
            }

            public static IEnumerable<Object[]> WarnMethods
            {
                get
                {
                    yield return new Object[] { CreateExpressionFor(log => log.Warn(new Exception())), new Exception().ToString() };
                    yield return new Object[] { CreateExpressionFor(log => log.Warn("My Test Message")), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.WarnFormat("My Test Message {0}", 0)), "My Test Message 0" };
                    yield return new Object[] { CreateExpressionFor(log => log.WarnFormat("My Test Message {0} {1}", 0, 1)), "My Test Message 0 1" };
                    yield return new Object[] { CreateExpressionFor(log => log.WarnFormat("My Test Message {0} {1} {2}", 0, 1, 2)), "My Test Message 0 1 2" };
                    yield return new Object[] { CreateExpressionFor(log => log.WarnFormat("My Test Message {0} {1} {2} {3}", 0, 1, 2, 3)), "My Test Message 0 1 2 3" };
                    yield return new Object[] { CreateExpressionFor(log => log.Warn(m => m("My Test Message"))), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.Warn(() => "My Test Message")), "My Test Message" };
                }
            }

            [Theory, PropertyData("WarnMethods")]
            public void LogMessageIfWarnEnabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Warning);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(1, listener.Messages.Count(m => m == expectedMessage));
            }

            [Theory, PropertyData("WarnMethods")]
            public void DoNotLogMessageIfWarnDisabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(0, listener.Messages.Count(m => m == expectedMessage));
            }
        }

        public class WhenLoggingInfo
        {
            private static Expression<Action<ILog>> CreateExpressionFor(Expression<Action<ILog>> expression)
            {
                return expression;
            }

            public static IEnumerable<Object[]> InfoMethods
            {
                get
                {
                    yield return new Object[] { CreateExpressionFor(log => log.Info(new Exception())), new Exception().ToString() };
                    yield return new Object[] { CreateExpressionFor(log => log.Info("My Test Message")), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.InfoFormat("My Test Message {0}", 0)), "My Test Message 0" };
                    yield return new Object[] { CreateExpressionFor(log => log.InfoFormat("My Test Message {0} {1}", 0, 1)), "My Test Message 0 1" };
                    yield return new Object[] { CreateExpressionFor(log => log.InfoFormat("My Test Message {0} {1} {2}", 0, 1, 2)), "My Test Message 0 1 2" };
                    yield return new Object[] { CreateExpressionFor(log => log.InfoFormat("My Test Message {0} {1} {2} {3}", 0, 1, 2, 3)), "My Test Message 0 1 2 3" };
                    yield return new Object[] { CreateExpressionFor(log => log.Info(m => m("My Test Message"))), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.Info(() => "My Test Message")), "My Test Message" };
                }
            }

            [Theory, PropertyData("InfoMethods")]
            public void LogMessageIfInfoEnabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Information);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(1, listener.Messages.Count(m => m == expectedMessage));
            }

            [Theory, PropertyData("InfoMethods")]
            public void DoNotLogMessageIfInfoDisabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(0, listener.Messages.Count(m => m == expectedMessage));
            }
        }

        public class WhenLoggingDebug
        {
            private static Expression<Action<ILog>> CreateExpressionFor(Expression<Action<ILog>> expression)
            {
                return expression;
            }

            public static IEnumerable<Object[]> DebugMethods
            {
                get
                {
                    yield return new Object[] { CreateExpressionFor(log => log.Debug(new Exception())), new Exception().ToString() };
                    yield return new Object[] { CreateExpressionFor(log => log.Debug("My Test Message")), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.DebugFormat("My Test Message {0}", 0)), "My Test Message 0" };
                    yield return new Object[] { CreateExpressionFor(log => log.DebugFormat("My Test Message {0} {1}", 0, 1)), "My Test Message 0 1" };
                    yield return new Object[] { CreateExpressionFor(log => log.DebugFormat("My Test Message {0} {1} {2}", 0, 1, 2)), "My Test Message 0 1 2" };
                    yield return new Object[] { CreateExpressionFor(log => log.DebugFormat("My Test Message {0} {1} {2} {3}", 0, 1, 2, 3)), "My Test Message 0 1 2 3" };
                    yield return new Object[] { CreateExpressionFor(log => log.Debug(m => m("My Test Message"))), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.Debug(() => "My Test Message")), "My Test Message" };
                }
            }

            [Theory, PropertyData("DebugMethods")]
            public void LogMessageIfDebugEnabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Verbose);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(1, listener.Messages.Count(m => m == expectedMessage));
            }

            [Theory, PropertyData("DebugMethods")]
            public void DoNotLogMessageIfDebugDisabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(0, listener.Messages.Count(m => m == expectedMessage));
            }
        }

        public class WhenLoggingTrace
        {
            private static Expression<Action<ILog>> CreateExpressionFor(Expression<Action<ILog>> expression)
            {
                return expression;
            }

            public static IEnumerable<Object[]> TraceMethods
            {
                get
                {
                    yield return new Object[] { CreateExpressionFor(log => log.Trace(new Exception())), new Exception().ToString() };
                    yield return new Object[] { CreateExpressionFor(log => log.Trace("My Test Message")), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.TraceFormat("My Test Message {0}", 0)), "My Test Message 0" };
                    yield return new Object[] { CreateExpressionFor(log => log.TraceFormat("My Test Message {0} {1}", 0, 1)), "My Test Message 0 1" };
                    yield return new Object[] { CreateExpressionFor(log => log.TraceFormat("My Test Message {0} {1} {2}", 0, 1, 2)), "My Test Message 0 1 2" };
                    yield return new Object[] { CreateExpressionFor(log => log.TraceFormat("My Test Message {0} {1} {2} {3}", 0, 1, 2, 3)), "My Test Message 0 1 2 3" };
                    yield return new Object[] { CreateExpressionFor(log => log.Trace(m => m("My Test Message"))), "My Test Message" };
                    yield return new Object[] { CreateExpressionFor(log => log.Trace(() => "My Test Message")), "My Test Message" };
                }
            }

            [Theory, PropertyData("TraceMethods")]
            public void LogMessageIfTraceEnabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.All);
                var testMethod = expression.Compile();

                lock (FakeTraceListener.Instance)
                {
                    Trace.Listeners.Add(FakeTraceListener.Instance);

                    try
                    {
                        testMethod(logger);

                        Assert.Equal(1, FakeTraceListener.Instance.Messages.Count(m => m == "MyTestLogger: " + expectedMessage));
                    }
                    finally
                    {
                        Trace.Listeners.Remove(FakeTraceListener.Instance);
                        FakeTraceListener.Instance.Clear();
                    }
                }
            }

            [Theory, PropertyData("TraceMethods")]
            public void DoNotLogMessageIfTraceDisabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(0, listener.Messages.Count(m => m == expectedMessage));
            }
        }

        public class WhenPushingDiagnosticContext
        {
            private static Expression<Func<ILog, IDisposable>> CreateExpressionFor(Expression<Func<ILog, IDisposable>> expression)
            {
                return expression;
            }

            private static Expression<Func<ILog, Guid, IDisposable>> CreateExpressionFor(Expression<Func<ILog, Guid, IDisposable>> expression)
            {
                return expression;
            }

            public static IEnumerable<Object[]> PushContextWithNoCorrelationId
            {
                get
                {
                    yield return new Object[] { CreateExpressionFor(log => log.PushContext("Name")) };
                    yield return new Object[] { CreateExpressionFor(log => log.PushContext("Name", 1)) };
                    yield return new Object[] { CreateExpressionFor(log => log.PushContext("Name", 1, 2)) };
                }
            }

            public static IEnumerable<Object[]> PushContextWitCorrelationId
            {
                get
                {
                    yield return new Object[] { CreateExpressionFor((log, activityId) => log.PushContext(activityId, "Name")) };
                    yield return new Object[] { CreateExpressionFor((log, activityId) => log.PushContext(activityId, "Name", 1)) };
                    yield return new Object[] { CreateExpressionFor((log, activityId) => log.PushContext(activityId, "Name", 1, 2)) };
                }
            }

            [Theory, PropertyData("PushContextWithNoCorrelationId")]
            public void DoNotChangeActivityIdIfNoCorrelationIdSpecified(Expression<Func<ILog, IDisposable>> expression)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.All);
                var pushContext = expression.Compile();
                var activityId = Guid.NewGuid();

                Trace.CorrelationManager.ActivityId = activityId;
                
                using (pushContext(logger))
                    Assert.Equal(activityId, Trace.CorrelationManager.ActivityId);

                Trace.CorrelationManager.ActivityId = Guid.Empty;
            }

            [Theory, PropertyData("PushContextWitCorrelationId")]
            public void ChangeActivityIdIfCorrelationIdSpecified(Expression<Func<ILog, Guid, IDisposable>> expression)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.All);
                var pushContext = expression.Compile();
                var activityId = Guid.NewGuid();

                Trace.CorrelationManager.ActivityId = Guid.Empty;

                using (pushContext(logger, activityId))
                    Assert.Equal(activityId, Trace.CorrelationManager.ActivityId);
            }
        }
    }
}
