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
    /// A <see cref="System.Diagnostics.Trace"/> based implementation of <see cref="ILog"/>.
    /// </summary>
    internal sealed class Logger : ILog
    {
        private const Int32 NoIdentifier = 0;
        private readonly Func<TraceSource, String, Object, Guid, IDisposable> diagnosticContextBuilder;
        private readonly TraceSource traceSource;
        private readonly Boolean fatalEnabled;
        private readonly Boolean errorEnabled;
        private readonly Boolean infoEnabled;
        private readonly Boolean warnEnabled;
        private readonly Boolean debugEnabled;
        private readonly Boolean traceEnabled;
        private volatile Boolean disposed;

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>FATAL</value> level messages; otherwise <value>false</value>.
        /// </summary>
        public Boolean IsFatalEnabled { get { return fatalEnabled; } }

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>ERROR</value> level messages; otherwise <value>false</value>.
        /// </summary>
        public Boolean IsErrorEnabled { get { return errorEnabled; } }

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>INFO</value> level messages; otherwise <value>false</value>.
        /// </summary>
        public Boolean IsInfoEnabled { get { return infoEnabled; } }

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>WARN</value> level messages; otherwise <value>false</value>.
        /// </summary>
        public Boolean IsWarnEnabled { get { return warnEnabled; } }

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>DEBUG</value> level messages; otherwise <value>false</value>.
        /// </summary>
        public Boolean IsDebugEnabled { get { return debugEnabled; } }

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>TRACE</value> level messages; otherwise <value>false</value>.
        /// </summary>
        public Boolean IsTraceEnabled { get { return traceEnabled; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class with the specified <paramref name="name"/> and logging <see cref="level"/>.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <param name="level">The log level of the logger.</param>
        public Logger(String name, SourceLevels level)
        {
            Verify.NotNullOrWhiteSpace(name, "name");

            traceSource = new TraceSource(name, level);
            fatalEnabled = (level & SourceLevels.Critical) == SourceLevels.Critical;
            errorEnabled = (level & SourceLevels.Error) == SourceLevels.Error;
            warnEnabled = (level & SourceLevels.Information) == SourceLevels.Information;
            infoEnabled = (level & SourceLevels.Warning) == SourceLevels.Warning;
            debugEnabled = (level & SourceLevels.Verbose) == SourceLevels.Verbose;
            traceEnabled = (level & SourceLevels.All) == SourceLevels.All;

            // Determine the diagnostic context that should be used if a context is pushed on to the logical call stack.
            // 
            // Benchmark to log 100,000 string messages (i.e., no formatting) with single context data parameter:
            //      NoContext (baseline)        -   ~1ms
            //      DisabledDiagnosticContext   -   ~3ms
            //      DefaultDiagnosticContext    -  ~80ms
            //      DebugDiagnosticContext      - ~260ms
            if ((level & SourceLevels.All) == SourceLevels.All)
                diagnosticContextBuilder = (source, context, data, correlationId) => new DebugDiagnosticContext(source, context, data, correlationId);
            else if ((level & SourceLevels.Verbose) == SourceLevels.Verbose || (level & SourceLevels.ActivityTracing) == SourceLevels.ActivityTracing)
                diagnosticContextBuilder = (source, context, data, correlationId) => new DefaultDiagnosticContext(source, context, data, correlationId);
            else
                diagnosticContextBuilder = (source, context, data, correlationId) => DisabledDiagnosticContext.Instance;
        }

        /// <summary>
        /// Releases all unmanaged resources used by the current instance of the <see cref="Logger"/> class.
        /// </summary>
        ~Logger()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="Logger"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///  Releases all resources used by the current instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(Boolean disposing)
        {
            if (!disposing || disposed)
                return;

            traceSource.Flush();
            traceSource.Close();
            disposed = true;
        }

        /// <summary>
        /// Writes a <value>FATAL</value> diagostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        public void Fatal(Exception ex)
        {
            if (fatalEnabled)
                traceSource.TraceData(TraceEventType.Critical, NoIdentifier, ex);
        }

        /// <summary>
        /// Writes a <value>FATAL</value> diagostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Fatal(String message)
        {
            if (fatalEnabled)
                traceSource.TraceEvent(TraceEventType.Critical, NoIdentifier, message);
        }

        /// <summary>
        /// Writes a <value>FATAL</value> diagostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        public void FatalFormat(String format, Object arg)
        {
            if (fatalEnabled)
                traceSource.TraceEvent(TraceEventType.Critical, NoIdentifier, String.Format(format, arg));
        }

        /// <summary>
        /// Writes a <value>FATAL</value> diagostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        public void FatalFormat(String format, Object arg0, Object arg1)
        {
            if (fatalEnabled)
                traceSource.TraceEvent(TraceEventType.Critical, NoIdentifier, String.Format(format, arg0, arg1));
        }

        /// <summary>
        /// Writes a <value>FATAL</value> diagostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        public void FatalFormat(String format, Object arg0, Object arg1, Object arg2)
        {
            if (fatalEnabled)
                traceSource.TraceEvent(TraceEventType.Critical, NoIdentifier, String.Format(format, arg0, arg1, arg2));
        }

        /// <summary>
        /// Writes a <value>FATAL</value> diagostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        public void FatalFormat(String format, params Object[] args)
        {
            if (fatalEnabled)
                traceSource.TraceEvent(TraceEventType.Critical, NoIdentifier, String.Format(format, args));
        }

        /// <summary>
        /// Writes a <value>FATAL</value> diagostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        public void Fatal(Func<String> messageBuilder)
        {
            if (fatalEnabled && messageBuilder != null)
                traceSource.TraceEvent(TraceEventType.Critical, NoIdentifier, messageBuilder());
        }

        /// <summary>
        /// Writes an <value>ERROR</value> diagostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        public void Error(Exception ex)
        {
            if (errorEnabled)
                traceSource.TraceData(TraceEventType.Error, NoIdentifier, ex);
        }

        /// <summary>
        /// Writes an <value>ERROR</value> diagostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Error(String message)
        {
            if (errorEnabled)
                traceSource.TraceEvent(TraceEventType.Error, NoIdentifier, message);
        }

        /// <summary>
        /// Writes an <value>ERROR</value> diagostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        public void ErrorFormat(String format, Object arg)
        {
            if (errorEnabled)
                traceSource.TraceEvent(TraceEventType.Error, NoIdentifier, String.Format(format, arg));
        }

        /// <summary>
        /// Writes an <value>ERROR</value> diagostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        public void ErrorFormat(String format, Object arg0, Object arg1)
        {
            if (errorEnabled)
                traceSource.TraceEvent(TraceEventType.Error, NoIdentifier, String.Format(format, arg0, arg1));
        }

        /// <summary>
        /// Writes an <value>ERROR</value> diagostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        public void ErrorFormat(String format, Object arg0, Object arg1, Object arg2)
        {
            if (errorEnabled)
                traceSource.TraceEvent(TraceEventType.Error, NoIdentifier, String.Format(format, arg0, arg1, arg2));
        }

        /// <summary>
        /// Writes an <value>ERROR</value> diagostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        public void ErrorFormat(String format, params Object[] args)
        {
            if (errorEnabled)
                traceSource.TraceEvent(TraceEventType.Error, NoIdentifier, String.Format(format, args));
        }

        /// <summary>
        /// Writes an <value>ERROR</value> diagostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        public void Error(Func<String> messageBuilder)
        {
            if (errorEnabled && messageBuilder != null)
                traceSource.TraceEvent(TraceEventType.Error, NoIdentifier, messageBuilder());
        }

        /// <summary>
        /// Writes a <value>WARN</value> diagostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        public void Warn(Exception ex)
        {
            if (warnEnabled)
                traceSource.TraceData(TraceEventType.Warning, NoIdentifier, ex);
        }

        /// <summary>
        /// Writes a <value>WARN</value> diagostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Warn(String message)
        {
            if (warnEnabled)
                traceSource.TraceEvent(TraceEventType.Warning, NoIdentifier, message);
        }

        /// <summary>
        /// Writes a <value>WARN</value> diagostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        public void WarnFormat(String format, Object arg)
        {
            if (warnEnabled)
                traceSource.TraceEvent(TraceEventType.Warning, NoIdentifier, String.Format(format, arg));
        }

        /// <summary>
        /// Writes a <value>WARN</value> diagostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        public void WarnFormat(String format, Object arg0, Object arg1)
        {
            if (warnEnabled)
                traceSource.TraceEvent(TraceEventType.Warning, NoIdentifier, String.Format(format, arg0, arg1));
        }

        /// <summary>
        /// Writes a <value>WARN</value> diagostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        public void WarnFormat(String format, Object arg0, Object arg1, Object arg2)
        {
            if (warnEnabled)
                traceSource.TraceEvent(TraceEventType.Warning, NoIdentifier, String.Format(format, arg0, arg1, arg2));
        }

        /// <summary>
        /// Writes a <value>WARN</value> diagostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        public void WarnFormat(String format, params Object[] args)
        {
            if (warnEnabled)
                traceSource.TraceEvent(TraceEventType.Warning, NoIdentifier, String.Format(format, args));
        }

        /// <summary>
        /// Writes a <value>WARN</value> diagostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        public void Warn(Func<String> messageBuilder)
        {
            if (warnEnabled && messageBuilder != null)
                traceSource.TraceEvent(TraceEventType.Warning, NoIdentifier, messageBuilder());
        }

        /// <summary>
        /// Writes an <value>INFO</value> diagostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        public void Info(Exception ex)
        {
            if (infoEnabled)
                traceSource.TraceData(TraceEventType.Information, NoIdentifier, ex);
        }

        /// <summary>
        /// Writes an <value>INFO</value> diagostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Info(String message)
        {
            if (infoEnabled)
                traceSource.TraceEvent(TraceEventType.Information, NoIdentifier, message);
        }

        /// <summary>
        /// Writes an <value>INFO</value> diagostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        public void InfoFormat(String format, Object arg)
        {
            if (infoEnabled)
                traceSource.TraceEvent(TraceEventType.Information, NoIdentifier, String.Format(format, arg));
        }

        /// <summary>
        /// Writes an <value>INFO</value> diagostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        public void InfoFormat(String format, Object arg0, Object arg1)
        {
            if (infoEnabled)
                traceSource.TraceEvent(TraceEventType.Information, NoIdentifier, String.Format(format, arg0, arg1));
        }

        /// <summary>
        /// Writes an <value>INFO</value> diagostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        public void InfoFormat(String format, Object arg0, Object arg1, Object arg2)
        {
            if (infoEnabled)
                traceSource.TraceEvent(TraceEventType.Information, NoIdentifier, String.Format(format, arg0, arg1, arg2));
        }

        /// <summary>
        /// Writes an <value>INFO</value> diagostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        public void InfoFormat(String format, params Object[] args)
        {
            if (infoEnabled)
                traceSource.TraceEvent(TraceEventType.Information, NoIdentifier, String.Format(format, args));
        }

        /// <summary>
        /// Writes an <value>INFO</value> diagostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        public void Info(Func<String> messageBuilder)
        {
            if (infoEnabled && messageBuilder != null)
                traceSource.TraceEvent(TraceEventType.Information, NoIdentifier, messageBuilder());
        }

        /// <summary>
        /// Writes a <value>DEBUG</value> diagostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        public void Debug(Exception ex)
        {
            if (debugEnabled)
                traceSource.TraceData(TraceEventType.Verbose, NoIdentifier, ex);
        }

        /// <summary>
        /// Writes a <value>DEBUG</value> diagostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Debug(String message)
        {
            if (debugEnabled)
                traceSource.TraceEvent(TraceEventType.Verbose, NoIdentifier, message);
        }

        /// <summary>
        /// Writes a <value>DEBUG</value> diagostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        public void DebugFormat(String format, Object arg)
        {
            if (debugEnabled)
                traceSource.TraceEvent(TraceEventType.Verbose, NoIdentifier, String.Format(format, arg));
        }

        /// <summary>
        /// Writes a <value>DEBUG</value> diagostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        public void DebugFormat(String format, Object arg0, Object arg1)
        {
            if (debugEnabled)
                traceSource.TraceEvent(TraceEventType.Verbose, NoIdentifier, String.Format(format, arg0, arg1));
        }

        /// <summary>
        /// Writes a <value>DEBUG</value> diagostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        public void DebugFormat(String format, Object arg0, Object arg1, Object arg2)
        {
            if (debugEnabled)
                traceSource.TraceEvent(TraceEventType.Verbose, NoIdentifier, String.Format(format, arg0, arg1, arg2));
        }

        /// <summary>
        /// Writes a <value>DEBUG</value> diagostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        public void DebugFormat(String format, params Object[] args)
        {
            if (debugEnabled)
                traceSource.TraceEvent(TraceEventType.Verbose, NoIdentifier, String.Format(format, args));
        }

        /// <summary>
        /// Writes a <value>DEBUG</value> diagostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        public void Debug(Func<String> messageBuilder)
        {
            if (debugEnabled && messageBuilder != null)
                traceSource.TraceEvent(TraceEventType.Verbose, NoIdentifier, messageBuilder());
        }

        /// <summary>
        /// Writes a <value>TRACE</value> diagostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        public void Trace(Exception ex)
        {
            if (traceEnabled)
                System.Diagnostics.Trace.WriteLine(ex, traceSource.Name);
        }

        /// <summary>
        /// Writes a <value>TRACE</value> diagostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Trace(String message)
        {
            if (traceEnabled)
                System.Diagnostics.Trace.WriteLine(message, traceSource.Name);
        }

        /// <summary>
        /// Writes a <value>TRACE</value> diagostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        public void TraceFormat(String format, Object arg)
        {
            if (traceEnabled)
                System.Diagnostics.Trace.WriteLine(String.Format(format, arg), traceSource.Name);
        }

        /// <summary>
        /// Writes a <value>TRACE</value> diagostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        public void TraceFormat(String format, Object arg0, Object arg1)
        {
            if (traceEnabled)
                System.Diagnostics.Trace.WriteLine(String.Format(format, arg0, arg1), traceSource.Name);
        }

        /// <summary>
        /// Writes a <value>TRACE</value> diagostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        public void TraceFormat(String format, Object arg0, Object arg1, Object arg2)
        {
            if (traceEnabled)
                System.Diagnostics.Trace.WriteLine(String.Format(format, arg0, arg1, arg2), traceSource.Name);
        }

        /// <summary>
        /// Writes a <value>TRACE</value> diagostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        public void TraceFormat(String format, params Object[] args)
        {
            if (traceEnabled)
                System.Diagnostics.Trace.WriteLine(String.Format(format, args), traceSource.Name);
        }

        /// <summary>
        /// Writes a <value>TRACE</value> diagostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        public void Trace(Func<String> messageBuilder)
        {
            if (traceEnabled && messageBuilder != null)
                System.Diagnostics.Trace.WriteLine(messageBuilder(), traceSource.Name);
        }

        /// <summary>
        /// Start a new named logical operation.
        /// </summary>
        /// <param name="name">The logical operation name.</param>
        public IDisposable PushContext(String name)
        {
            return diagnosticContextBuilder(traceSource, name, null, Guid.Empty);
        }

        /// <summary>
        /// Start a new named logical operation.
        /// </summary>
        /// <param name="name">The logical operation name.</param>
        /// <param name="data">The logical operation context.</param>
        public IDisposable PushContext(String name, Object data)
        {
            return diagnosticContextBuilder(traceSource, name, data, Guid.Empty);
        }

        /// <summary>
        /// Start a new named logical operation.
        /// </summary>
        /// <param name="name">The logical operation name.</param>
        /// <param name="data">The logical operation context.</param>
        public IDisposable PushContext(String name, params Object[] data)
        {
            return diagnosticContextBuilder(traceSource, name, data, Guid.Empty);
        }

        /// <summary>
        /// Start a new named logical operation associated with the specified <paramref name="correlationId"/>.
        /// </summary>
        /// <param name="correlationId">The logical operation correlation id.</param>
        /// <param name="name">The logical operation name.</param>
        public IDisposable PushContext(Guid correlationId, String name)
        {
            return diagnosticContextBuilder(traceSource, name, null, correlationId);
        }

        /// <summary>
        /// Start a new named logical operation associated with the specified <paramref name="correlationId"/>.
        /// </summary>
        /// <param name="correlationId">The logical operation correlation id.</param>
        /// <param name="name">The logical operation name.</param>
        /// <param name="data">The logical operation context.</param>
        public IDisposable PushContext(Guid correlationId, String name, Object data)
        {
            return diagnosticContextBuilder(traceSource, name, data, correlationId);
        }

        /// <summary>
        /// Start a new named logical operation associated with the specified <paramref name="correlationId"/>.
        /// </summary>
        /// <param name="correlationId">The logical operation correlation id.</param>
        /// <param name="name">The logical operation name.</param>
        /// <param name="data">The logical operation context.</param>
        public IDisposable PushContext(Guid correlationId, String name, params Object[] data)
        {
            return diagnosticContextBuilder(traceSource, name, data, correlationId);
        }
    }
}
