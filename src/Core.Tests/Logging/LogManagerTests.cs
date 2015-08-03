using System;
using System.Diagnostics;
using Spark.Logging;
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
    namespace UsingLogManager
    {
        public class WhenCreatingLogger
        {
            [Fact]
            public void DefaultToWarningIfSwitchNameUnknown()
            {
                var logManager = new LogManager();
                var logger = logManager.CreateLogger("LogManager.UnknownSource");

                Assert.True(logger.IsWarnEnabled);
                Assert.False(logger.IsInfoEnabled);
            }

            [Fact]
            public void DefaultToStandardListenersIfSwitchNameUnknown()
            {
                var logManager = new LogManager();
                var logger = (Logger)logManager.CreateLogger("LogManager.UnknownSource");

                Assert.Equal(Trace.Listeners.Count, logger.TraceSource.Listeners.Count);
            }

            [Fact]
            public void OverrideDefaultLevelIfSwitchConfigured()
            {
                var logManager = new LogManager();
                var logger = logManager.CreateLogger("LogManager.Test");

                Assert.True(logger.IsDebugEnabled);
            }

            [Fact]
            public void OverrideListenersIfSourceConfigured()
            {
                var logManager = new LogManager();
                var logger = (Logger)logManager.CreateLogger("LogManager.Test");

                Assert.Equal(0, logger.TraceSource.Listeners.Count);
            }

            [Fact]
            public void TreatPeriodAsNamespaceMarker()
            {
                var logManager = new LogManager();
                var logger = (Logger)logManager.CreateLogger("LogManager.Test.Namespace");

                Assert.True(logger.IsDebugEnabled);
                Assert.Equal(0, logger.TraceSource.Listeners.Count);
            }

            [Theory, InlineData("My..Test"), InlineData("My.Test."), InlineData(".Test.Logger")]
            public void CanTolerateBadLoggerName(String name)
            {
                var logManager = new LogManager();

                Assert.NotNull(logManager.CreateLogger(name));
            }

            [Theory, InlineData("LogManager.TEST"), InlineData("LogManager.Test"), InlineData("LogManager.test")]
            public void SwitchNameIsCaseInsensitive(String name)
            {
                var logManager = new LogManager();
                var logger = (Logger)logManager.CreateLogger(name);

                Assert.True(logger.IsDebugEnabled);
                Assert.Equal(0, logger.TraceSource.Listeners.Count);
            }
        }

        public class WhenGettingCurrentClassLogger
        {
            [Fact]
            public void LoggerIsNamedAfterDeclaringClassFullName()
            {
                Assert.Equal("Test.Spark.Logging.UsingLogManager.LoggerClass", LoggerClass.Log.Name);
            }
        }

        public class WhenGettingNamedLogger
        {
            [Fact]
            public void LoggerIsNamedAsSpecified()
            {
                var logger = LogManager.GetLogger("MyCustomName");

                Assert.Equal("MyCustomName", logger.Name);
            }

            [Fact]
            public void LoggerIsNamedAsFullTypeName()
            {
                var logger = LogManager.GetLogger(typeof(WhenGettingNamedLogger));

                Assert.Equal("Test.Spark.Logging.UsingLogManager.WhenGettingNamedLogger", logger.Name);
            }
        }

        internal sealed class LoggerClass
        {
            public static readonly ILog Log = LogManager.GetCurrentClassLogger();
        }
    }
}
