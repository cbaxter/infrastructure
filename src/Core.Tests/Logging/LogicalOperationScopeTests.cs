using System.Linq;
using Spark;
using Spark.Logging;
using Spark.Resources;
using System;
using System.Diagnostics;
using System.Reflection;
using Xunit;

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

namespace Test.Spark.Logging
{
    namespace UsingLogicalOperationScope
    {
        public class WhenCreatingNewScope
        {
            [Fact]
            public void NewLogicalOperationPushedOnToStack()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);

                using (new LogicalOperationScope(traceSource, traceSource.Name))
                    Assert.Equal("NewLogicalOperationPushedOnToStack", Trace.CorrelationManager.LogicalOperationStack.Peek());
            }

            [Fact]
            public void TraceStartEventWhenTracingEnabled()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var listener = new FakeTraceListener();

                traceSource.Listeners.Add(listener);
                using (new LogicalOperationScope(traceSource, traceSource.Name, traceEnabled: true))
                    Assert.Equal(1, listener.Messages.Count(m => m.Trim() == $"Logical operation {traceSource.Name} started"));
            }

            [Fact]
            public void DoNotTraceStartEventWhenTracingDisabled()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var listener = new FakeTraceListener();

                traceSource.Listeners.Add(listener);
                using (new LogicalOperationScope(traceSource, traceSource.Name, traceEnabled: false))
                    Assert.Equal(0, listener.Messages.Count(m => m.Trim() == $"Logical operation {traceSource.Name} started"));
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnName()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);

                using (var context = new LogicalOperationScope(traceSource, traceSource.Name))
                    Assert.Equal(traceSource.Name, context.ToString());
            }
        }

        // ReSharper disable AccessToDisposedClosure
        public class WhenDisposingExistingDiagnosticContext
        {
            [Fact]
            public void CanDisposeRepeatedly()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                
                using (var context = new LogicalOperationScope(traceSource, traceSource.Name))
                    context.Dispose();
            }

            [Fact]
            public void CannotInterleaveLogicalOperations()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var context = new LogicalOperationScope(traceSource, traceSource.Name);

                Trace.CorrelationManager.LogicalOperationStack.Push(Guid.NewGuid());

                var ex = Assert.Throws<InvalidOperationException>(() => context.Dispose());
                Assert.Equal(Exceptions.OperationIdModifiedInsideScope, ex.Message);

                Trace.CorrelationManager.LogicalOperationStack.Pop();

                context.Dispose();
            }
            
            [Fact]
            public void FinalizerIgnoresContextInterleave()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var finalizer = typeof(LogicalOperationScope).GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

                Trace.CorrelationManager.ActivityId = Guid.Empty;
                using (var context = new LogicalOperationScope(traceSource, traceSource.Name))
                {
                    Trace.CorrelationManager.ActivityId = Guid.NewGuid();
                    finalizer.Invoke(context, null);
                    Trace.CorrelationManager.ActivityId = Guid.Empty;
                }
            }

            [Fact]
            public void LogicalOperationPoppedFromStack()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);

                using (new LogicalOperationScope(traceSource, traceSource.Name + " #1"))
                {
                    using (new LogicalOperationScope(traceSource, traceSource.Name + " #2"))
                        Assert.Equal(traceSource.Name + " #2", Trace.CorrelationManager.LogicalOperationStack.Peek());

                    Assert.Equal(traceSource.Name + " #1", Trace.CorrelationManager.LogicalOperationStack.Peek()); 
                }
            }
            
            [Fact]
            public void TraceStopEventWhenTracingEnabled()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var listener = new FakeTraceListener();

                traceSource.Listeners.Add(listener);
                new LogicalOperationScope(traceSource, traceSource.Name, traceEnabled: true).Dispose();

                Assert.Equal(1, listener.Messages.Count(m => m.Trim() == $"Logical operation {traceSource.Name} stopped"));
            }

            [Fact]
            public void DoNotTraceStopEventWhenTracingDisabled()
            {
                var traceSource = new TraceSource(MethodBase.GetCurrentMethod().Name, SourceLevels.All);
                var listener = new FakeTraceListener();

                traceSource.Listeners.Add(listener);
                new LogicalOperationScope(traceSource, traceSource.Name, traceEnabled: false).Dispose();

                Assert.Equal(0, listener.Messages.Count(m => m.Trim() == $"Logical operation {traceSource.Name} stopped"));
            }
        }
        // ReSharper restore AccessToDisposedClosure
    }
}
