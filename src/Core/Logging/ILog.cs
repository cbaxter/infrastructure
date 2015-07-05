using System;

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

namespace Spark.Logging
{
    /// <summary>
    /// Provides logging interface.
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Returns the name of this <see cref="Logger"/> instance.
        /// </summary>
        String Name { get; }

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>FATAL</value> level messages; otherwise <value>false</value>.
        /// </summary>
        Boolean IsFatalEnabled { get; }

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>ERROR</value> level messages; otherwise <value>false</value>.
        /// </summary>
        Boolean IsErrorEnabled { get; }

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>INFO</value> level messages; otherwise <value>false</value>.
        /// </summary>
        Boolean IsInfoEnabled { get; }

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>WARN</value> level messages; otherwise <value>false</value>.
        /// </summary>
        Boolean IsWarnEnabled { get; }

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>DEBUG</value> level messages; otherwise <value>false</value>.
        /// </summary>
        Boolean IsDebugEnabled { get; }

        /// <summary>
        /// Returns <value>true</value> if logging is enabled for <value>TRACE</value> level messages; otherwise <value>false</value>.
        /// </summary>
        Boolean IsTraceEnabled { get; }

        /// <summary>
        /// Writes a <value>FATAL</value> diagnostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        void Fatal(Exception ex);

        /// <summary>
        /// Writes a <value>FATAL</value> diagnostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Fatal(String message);

        /// <summary>
        /// Writes a <value>FATAL</value> diagnostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        void FatalFormat(String format, Object arg);

        /// <summary>
        /// Writes a <value>FATAL</value> diagnostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        void FatalFormat(String format, Object arg0, Object arg1);

        /// <summary>
        /// Writes a <value>FATAL</value> diagnostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        void FatalFormat(String format, Object arg0, Object arg1, Object arg2);

        /// <summary>
        /// Writes a <value>FATAL</value> diagnostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        void FatalFormat(String format, params Object[] args);

        /// <summary>
        /// Writes a <value>FATAL</value> diagnostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Fatal(Func<String> messageBuilder);

        /// <summary>
        /// Writes a <value>FATAL</value> diagnostic message if <value>IsFatalEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Fatal(Func<FormatMessageHandler, String> messageBuilder);

        /// <summary>
        /// Writes an <value>ERROR</value> diagnostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        void Error(Exception ex);

        /// <summary>
        /// Writes an <value>ERROR</value> diagnostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Error(String message);

        /// <summary>
        /// Writes an <value>ERROR</value> diagnostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        void ErrorFormat(String format, Object arg);

        /// <summary>
        /// Writes an <value>ERROR</value> diagnostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        void ErrorFormat(String format, Object arg0, Object arg1);

        /// <summary>
        /// Writes an <value>ERROR</value> diagnostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        void ErrorFormat(String format, Object arg0, Object arg1, Object arg2);

        /// <summary>
        /// Writes an <value>ERROR</value> diagnostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        void ErrorFormat(String format, params Object[] args);

        /// <summary>
        /// Writes an <value>ERROR</value> diagnostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Error(Func<String> messageBuilder);

        /// <summary>
        /// Writes an <value>ERROR</value> diagnostic message if <value>IsErrorEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Error(Func<FormatMessageHandler, String> messageBuilder);

        /// <summary>
        /// Writes a <value>WARN</value> diagnostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        void Warn(Exception ex);

        /// <summary>
        /// Writes a <value>WARN</value> diagnostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Warn(String message);

        /// <summary>
        /// Writes a <value>WARN</value> diagnostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        void WarnFormat(String format, Object arg);

        /// <summary>
        /// Writes a <value>WARN</value> diagnostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        void WarnFormat(String format, Object arg0, Object arg1);

        /// <summary>
        /// Writes a <value>WARN</value> diagnostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        void WarnFormat(String format, Object arg0, Object arg1, Object arg2);

        /// <summary>
        /// Writes a <value>WARN</value> diagnostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        void WarnFormat(String format, params Object[] args);

        /// <summary>
        /// Writes a <value>WARN</value> diagnostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Warn(Func<String> messageBuilder);

        /// <summary>
        /// Writes a <value>WARN</value> diagnostic message if <value>IsWarnEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Warn(Func<FormatMessageHandler, String> messageBuilder);

        /// <summary>
        /// Writes an <value>INFO</value> diagnostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        void Info(Exception ex);

        /// <summary>
        /// Writes an <value>INFO</value> diagnostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Info(String message);

        /// <summary>
        /// Writes an <value>INFO</value> diagnostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        void InfoFormat(String format, Object arg);

        /// <summary>
        /// Writes an <value>INFO</value> diagnostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        void InfoFormat(String format, Object arg0, Object arg1);

        /// <summary>
        /// Writes an <value>INFO</value> diagnostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        void InfoFormat(String format, Object arg0, Object arg1, Object arg2);

        /// <summary>
        /// Writes an <value>INFO</value> diagnostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        void InfoFormat(String format, params Object[] args);

        /// <summary>
        /// Writes an <value>INFO</value> diagnostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Info(Func<String> messageBuilder);

        /// <summary>
        /// Writes an <value>INFO</value> diagnostic message if <value>IsInfoEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Info(Func<FormatMessageHandler, String> messageBuilder);

        /// <summary>
        /// Writes a <value>DEBUG</value> diagnostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        void Debug(Exception ex);

        /// <summary>
        /// Writes a <value>DEBUG</value> diagnostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Debug(String message);

        /// <summary>
        /// Writes a <value>DEBUG</value> diagnostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        void DebugFormat(String format, Object arg);

        /// <summary>
        /// Writes a <value>DEBUG</value> diagnostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        void DebugFormat(String format, Object arg0, Object arg1);

        /// <summary>
        /// Writes a <value>DEBUG</value> diagnostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        void DebugFormat(String format, Object arg0, Object arg1, Object arg2);

        /// <summary>
        /// Writes a <value>DEBUG</value> diagnostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        void DebugFormat(String format, params Object[] args);

        /// <summary>
        /// Writes a <value>DEBUG</value> diagnostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Debug(Func<String> messageBuilder);

        /// <summary>
        /// Writes a <value>DEBUG</value> diagnostic message if <value>IsDebugEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Debug(Func<FormatMessageHandler, String> messageBuilder);

        /// <summary>
        /// Writes a <value>TRACE</value> diagnostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to log.</param>
        void Trace(Exception ex);

        /// <summary>
        /// Writes a <value>TRACE</value> diagnostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Trace(String message);

        /// <summary>
        /// Writes a <value>TRACE</value> diagnostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg">The <see cref="Object"/> to format.</param>
        void TraceFormat(String format, Object arg);

        /// <summary>
        /// Writes a <value>TRACE</value> diagnostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        void TraceFormat(String format, Object arg0, Object arg1);

        /// <summary>
        /// Writes a <value>TRACE</value> diagnostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="arg0">The first <see cref="Object"/> to format.</param>
        /// <param name="arg1">The second <see cref="Object"/> to format.</param>
        /// <param name="arg2">The third <see cref="Object"/> to format.</param>
        void TraceFormat(String format, Object arg0, Object arg1, Object arg2);

        /// <summary>
        /// Writes a <value>TRACE</value> diagnostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="format">A composite format <see cref="String"/>.</param>
        /// <param name="args">An <see cref="Object"/> array that contains zero or more objects to format.</param>
        void TraceFormat(String format, params Object[] args);

        /// <summary>
        /// Writes a <value>TRACE</value> diagnostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Trace(Func<String> messageBuilder);

        /// <summary>
        /// Writes a <value>TRACE</value> diagnostic message if <value>IsTraceEnabled</value> is <value>true</value>; otherwise ignored.
        /// </summary>
        /// <param name="messageBuilder">A <see cref="Func{String}"/> message builder.</param>
        void Trace(Func<FormatMessageHandler, String> messageBuilder);

        /// <summary>
        /// Start a new named logical operation.
        /// </summary>
        /// <param name="name">The logical operation name.</param>
        IDisposable PushContext(String name);

        /// <summary>
        /// Start a new activity scope.
        /// </summary>
        /// <param name="activityId">The activity identifier.</param>
        IDisposable Transfer(Guid activityId);
    }
}
