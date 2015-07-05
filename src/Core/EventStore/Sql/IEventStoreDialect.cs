using System;
using System.Data;
using Spark.Data;

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

#pragma warning disable 1591
namespace Spark.EventStore.Sql
{
    /// <summary>
    /// The SQL RDBMS dialect statements associated with an <see cref="SqlEventStore"/> instance.
    /// </summary>
    public interface IEventStoreDialect : IDbDialect
    {
        String GetRange { get; }
        String GetStream { get; }
        String GetStreams { get; }
        String GetUndispatched { get; }
        String MarkDispatched { get; }
        String InsertCommit { get; }
        String UpdateCommit { get; }
        String DeleteStream { get; }
        String DeleteStreams { get; }

        String EnsureCommitTableExists { get; }
        String EnsureDuplicateCommitsDetected { get; }
        String EnsureDuplicateCommitsSuppressed { get; }

        IDataParameter CreateIdParameter(Int64 id);
        IDataParameter CreateTimestampParameter(DateTime timestamp);
        IDataParameter CreateCorrelationIdParameter(Guid correlationId);
        IDataParameter CreateStreamIdParameter(Guid streamId);
        IDataParameter CreateVersionParameter(Int32 version);
        IDataParameter CreateDataParameter(Byte[] data);
        IDataParameter CreateSkipParameter(Int64 skip);
        IDataParameter CreateTakeParameter(Int64 take);
    }
}
#pragma warning restore 1591
