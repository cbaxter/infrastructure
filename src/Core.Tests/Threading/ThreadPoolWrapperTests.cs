using System;
using System.Threading;
using Spark.Infrastructure.Threading;
using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace Spark.Infrastructure.Tests.Threading
{
    public static class UsingThreadPoolWrapper
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
