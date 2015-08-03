using System;
using System.Threading;
using Spark;
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

namespace Test.Spark.Threading
{
    namespace UsingExponentialBackoff
    {
        public class WhenCheckingCanRetry
        {
            [Fact]
            public void ReturnTrueIfSystemTimeLessThanTimeout()
            {
                var now = DateTime.UtcNow;

                SystemTime.OverrideWith(() => now);

                var backoff = new ExponentialBackoff(TimeSpan.FromMinutes(1));

                Assert.True(backoff.CanRetry);
            }

            [Fact]
            public void ReturnFalseIfSystemTimeGreaterThanTimeout()
            {
                var now = DateTime.UtcNow;

                SystemTime.OverrideWith(() => now);

                var backoff = new ExponentialBackoff(TimeSpan.FromMinutes(1));

                SystemTime.OverrideWith(() => now.AddMinutes(2));

                Assert.False(backoff.CanRetry);
            }
        }

        public class WhenWaitingUntilRetry
        {
            [Fact]
            public void FirstWaitShouldNotSleep()
            {
                SystemTime.ClearOverride();

                var backoff = new ExponentialBackoff(TimeSpan.FromMilliseconds(100));

                Assert.Equal(TimeSpan.Zero, backoff.WaitUntilRetry());
            }

            [Fact]
            public void SubsequentWaitsShouldSleep()
            {
                SystemTime.ClearOverride();

                var backoff = new ExponentialBackoff(TimeSpan.FromMilliseconds(100));

                backoff.WaitUntilRetry();

                Assert.NotEqual(TimeSpan.Zero, backoff.WaitUntilRetry());
            }

            [Fact]
            public void SleepCannotExceedMaximumWait()
            {
                SystemTime.ClearOverride();

                var backoff = new ExponentialBackoff(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5));

                backoff.WaitUntilRetry();

                Assert.InRange(backoff.WaitUntilRetry(), TimeSpan.FromMilliseconds(3), TimeSpan.FromMilliseconds(5));
            }

            [Fact]
            public void SleepCannotExceedTimeRemaining()
            {
                SystemTime.ClearOverride();

                var backoff = new ExponentialBackoff(TimeSpan.FromMilliseconds(5), TimeSpan.FromSeconds(1));

                backoff.WaitUntilRetry();

                Assert.InRange(backoff.WaitUntilRetry(), TimeSpan.FromMilliseconds(3), TimeSpan.FromMilliseconds(5));
            }
        }

        public class WhenWaitOrTimeout
        {
            public WhenWaitOrTimeout()
            {
                SystemTime.ClearOverride();
            }

            [Fact]
            public void WaitIfCanRetry()
            {
                var backoff = new ExponentialBackoff(TimeSpan.FromMilliseconds(100));

                backoff.WaitOrTimeout(new Exception());
            }

            [Fact]
            public void TimeoutIfCannotRetry()
            {
                var backoff = new ExponentialBackoff(TimeSpan.FromMilliseconds(1));

                Thread.Sleep(20);

                Assert.Throws<TimeoutException>(() => backoff.WaitOrTimeout(new Exception()));
            }
        }
    }
}
