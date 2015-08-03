using System;
using System.Threading;
using Spark.Threading;
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

// ReSharper disable AccessToDisposedClosure
namespace Test.Spark.Threading
{
    namespace UsingThreadPoolWrapper
    {
        public class WhenQueuingUserWorkItem
        {
            [Fact]
            public void UseThreadPoolThread()
            {
                using (var manualResetEvent = new ManualResetEvent(false))
                {
                    var usedThreadPoolThread = false;

                    ThreadPoolWrapper.Instance.QueueUserWorkItem(() =>
                        {
                            usedThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread;
                            manualResetEvent.Set();
                        });

                    Assert.True(manualResetEvent.WaitOne(TimeSpan.FromMilliseconds(100)));
                    Assert.True(usedThreadPoolThread);
                }
            }
        }

        public class WhenQueuingUserWorkItemWithState
        {
            [Fact]
            public void UseThreadPoolThread()
            {
                using (var manualResetEvent = new ManualResetEvent(false))
                {
                    var usedThreadPoolThread = false;

                    ThreadPoolWrapper.Instance.QueueUserWorkItem(state =>
                        {
                            usedThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread;
                            manualResetEvent.Set();
                        }, new Object());

                    Assert.True(manualResetEvent.WaitOne(TimeSpan.FromMilliseconds(100)));
                    Assert.True(usedThreadPoolThread);
                }

            }
        }

        public class WhenQueuingUnsafeUserWorkItem
        {
            [Fact]
            public void UseThreadPoolThread()
            {
                using (var manualResetEvent = new ManualResetEvent(false))
                {
                    var usedThreadPoolThread = false;

                    ThreadPoolWrapper.Instance.UnsafeQueueUserWorkItem(() =>
                        {
                            usedThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread;
                            manualResetEvent.Set();
                        });

                    Assert.True(manualResetEvent.WaitOne(TimeSpan.FromMilliseconds(100)));
                    Assert.True(usedThreadPoolThread);
                }

            }
        }

        public class WhenQueuingUnsafeUserWorkItemWithState
        {
            [Fact]
            public void UseThreadPoolThread()
            {
                using (var manualResetEvent = new ManualResetEvent(false))
                {
                    var usedThreadPoolThread = false;

                    ThreadPoolWrapper.Instance.UnsafeQueueUserWorkItem(state =>
                        {
                            usedThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread;
                            manualResetEvent.Set();
                        }, new Object());

                    Assert.True(manualResetEvent.WaitOne(TimeSpan.FromMilliseconds(100)));
                    Assert.True(usedThreadPoolThread);
                }

            }
        }
    }
}
