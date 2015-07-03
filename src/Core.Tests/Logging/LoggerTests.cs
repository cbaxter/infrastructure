using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Spark.Logging;
using Xunit;

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

namespace Test.Spark.Logging
{
    namespace UsingLogger
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

            [Theory, MemberData("FatalMethods")]
            public void LogMessageIfFatalEnabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Critical);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(1, listener.Messages.Count(m => m == expectedMessage));
            }

            [Theory, MemberData("FatalMethods")]
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

            [Theory, MemberData("ErrorMethods")]
            public void LogMessageIfErrorEnabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Error);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(1, listener.Messages.Count(m => m == expectedMessage));
            }

            [Theory, MemberData("ErrorMethods")]
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

            [Theory, MemberData("WarnMethods")]
            public void LogMessageIfWarnEnabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Warning);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(1, listener.Messages.Count(m => m == expectedMessage));
            }

            [Theory, MemberData("WarnMethods")]
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

            [Theory, MemberData("InfoMethods")]
            public void LogMessageIfInfoEnabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Information);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(1, listener.Messages.Count(m => m == expectedMessage));
            }

            [Theory, MemberData("InfoMethods")]
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

            [Theory, MemberData("DebugMethods")]
            public void LogMessageIfDebugEnabled(Expression<Action<ILog>> expression, String expectedMessage)
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Verbose);
                var listener = new FakeTraceListener();
                var testMethod = expression.Compile();

                logger.TraceSource.Listeners.Add(listener);

                testMethod(logger);

                Assert.Equal(1, listener.Messages.Count(m => m == expectedMessage));
            }

            [Theory, MemberData("DebugMethods")]
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

            [Theory, MemberData("TraceMethods")]
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

            [Theory, MemberData("TraceMethods")]
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

        public class WhenTransfering
        {
            [Fact]
            public void AlwaysReturnActivityScope()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);

                Assert.IsType(typeof(ActivityScope), logger.Transfer(Guid.NewGuid()));
            }
        }

        public class WhenPushingContext
        {
            [Fact]
            public void AlwaysReturnLogicalOperationScope()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);

                Assert.IsType(typeof(LogicalOperationScope), logger.PushContext());
            }

            [Fact]
            public void CanPushContextWithFixedName()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);

                using (logger.PushContext("Name"))
                    Assert.Equal("Name", Trace.CorrelationManager.LogicalOperationStack.Peek());
            }

            [Fact]
            public void CanPushContextWithSingleNameParameter()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);

                using (logger.PushContext("Name {0}", 1))
                    Assert.Equal("Name 1", Trace.CorrelationManager.LogicalOperationStack.Peek());
            }

            [Fact]
            public void CanPushContextWithTwoNameParameters()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);

                using (logger.PushContext("Name {0} {1}", 1, 2))
                    Assert.Equal("Name 1 2", Trace.CorrelationManager.LogicalOperationStack.Peek());
            }

            [Fact]
            public void CanPushContextWithThreeNameParametes()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);

                using (logger.PushContext("Name {0} {1} {2}", 1, 2, 3))
                    Assert.Equal("Name 1 2 3", Trace.CorrelationManager.LogicalOperationStack.Peek());
            }

            [Fact]
            public void CanPushContextWithManyNameParameters()
            {
                var logger = new Logger("MyTestLogger", SourceLevels.Off);

                using (logger.PushContext("Name {0} {1} {2} {3} {4}", 1, 2, 3, 4, 5))
                    Assert.Equal("Name 1 2 3 4 5", Trace.CorrelationManager.LogicalOperationStack.Peek());
            }
        }
    }
}
