using System;
using System.Collections.Generic;
using System.Data;
using Spark.Configuration;
using Spark.Cqrs.Eventing;
using Spark.Data;
using Spark.Logging;
using Spark.Messaging;
using Spark.Serialization;

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

namespace Spark.EventStore.Sql
{
    /// <summary>
    /// An RDBMS event store.
    /// </summary>
    public sealed class SqlEventStore : IStoreEvents, IDisposable
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly DbBatchOperation dispatchedBuffer;
        private readonly Boolean detectDuplicateCommits;
        private readonly ISerializeObjects serializer;
        private readonly IEventStoreDialect dialect;
        private readonly Boolean useAsyncWrite;
        private readonly Int64 pageSize;
        private Boolean disposed;

        private static class Column
        {
            public const Int32 Id = 0;
            public const Int32 Timestamp = 1;
            public const Int32 CorrelationId = 2;
            public const Int32 StreamId = 3;
            public const Int32 Version = 4;
            public const Int32 Data = 5;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SqlEventStore"/>.
        /// </summary>
        /// <param name="dialect">The database dialect associated with this <see cref="SqlEventStore"/>.</param>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> used to store binary data.</param>
        public SqlEventStore(IEventStoreDialect dialect, ISerializeObjects serializer)
            : this(dialect, serializer, Settings.EventStore)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="SqlEventStore"/> with a custom <see cref="IEventStoreDialect"/>.
        /// </summary>
        /// <param name="dialect">The database dialect associated with this <see cref="SqlEventStore"/>.</param>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> used to store binary data.</param>
        /// <param name="settings">The event store settings.</param>
        internal SqlEventStore(IEventStoreDialect dialect, ISerializeObjects serializer, IStoreEventSettings settings)
        {
            Verify.NotNull(serializer, nameof(serializer));
            Verify.NotNull(settings, nameof(settings));
            Verify.NotNull(dialect, nameof(dialect));

            this.dialect = dialect;
            this.serializer = serializer;
            this.pageSize = settings.PageSize;
            this.useAsyncWrite = settings.Async;
            this.detectDuplicateCommits = settings.DetectDuplicateCommits;
            this.dispatchedBuffer = settings.Async ? CreateBuffer(settings, dialect) : null;

            Initialize();
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="SqlSnapshotStore"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            if (useAsyncWrite)
                dispatchedBuffer.Dispose();
        }

        /// <summary>
        /// Creates a <see cref="DataTable"/> based on the required insert/update command parameters.
        /// </summary>
        private static DbBatchOperation CreateBuffer(IStoreEventSettings settings, IEventStoreDialect dialect)
        {
            using (var command = dialect.CreateCommand(dialect.MarkDispatched))
            {
                command.Parameters.Add(dialect.CreateIdParameter(default(Int64)));

                return new DbBatchOperation(dialect, command, settings.BatchSize, settings.FlushInterval);
            }
        }

        /// <summary>
        /// Initializes a new event store.
        /// </summary>
        private void Initialize()
        {
            Log.Trace("Initializing event store");

            using (var command = dialect.CreateCommand(dialect.EnsureCommitTableExists))
                dialect.ExecuteNonQuery(command);

            // NOTE: Most durable message queues ensure `at least once` delivery of messages; however when using an internal message 
            //       queue (i.e., BlockingCollection) or non-durable message queue (i.e., IPC via named pipes) duplicate commits may 
            //       not need to be enforced. Allow additional commit ID unique index be created/droped as desired.
            using (var command = dialect.CreateCommand(detectDuplicateCommits ? dialect.EnsureDuplicateCommitsDetected : dialect.EnsureDuplicateCommitsSuppressed))
                dialect.ExecuteNonQuery(command);
        }

        /// <summary>
        /// Get all undispatched commits.
        /// </summary>
        public IEnumerable<Commit> GetUndispatched()
        {
            Verify.NotDisposed(this, disposed);

            return new PagedResult<Commit>(pageSize, (lastResult, page) => GetUndispatchedAfter(lastResult != null && lastResult.Id.HasValue ? lastResult.Id.Value : 0, page.Take));
        }

        /// <summary>
        /// Get a specified undispatched commit range starting after <paramref name="skip"/> and returing <paramref name="take"/> commits.
        /// </summary>
        /// <param name="skip">The commit id lower bound (exclusive).</param>
        /// <param name="take">The number of commits to include in the result.</param>
        private IEnumerable<Commit> GetUndispatchedAfter(Int64 skip, Int64 take)
        {
            using (var command = dialect.CreateCommand(dialect.GetUndispatched))
            {
                Log.TraceFormat("Getting next {0} undispatched commits after {1}", take, skip);

                command.Parameters.Add(dialect.CreateSkipParameter(skip + 1));
                command.Parameters.Add(dialect.CreateTakeParameter(take));

                return dialect.QueryMultiple(command, CreateCommit);
            }
        }

        /// <summary>
        /// Get the specified commit range starting after <paramref name="skip"/> and returing <paramref name="take"/> commits.
        /// </summary>
        /// <param name="skip">The commit id lower bound (exclusive).</param>
        /// <param name="take">The number of commits to include in the result.</param>
        public IReadOnlyList<Commit> GetRange(Int64 skip, Int64 take)
        {
            Verify.NotDisposed(this, disposed);
            Verify.GreaterThan(0, take, nameof(take));
            Verify.GreaterThanOrEqual(0, skip, nameof(skip));

            using (var command = dialect.CreateCommand(dialect.GetRange))
            {
                Log.TraceFormat("Getting next {0} commits after {1}", take, skip);

                command.Parameters.Add(dialect.CreateSkipParameter(skip + 1));
                command.Parameters.Add(dialect.CreateTakeParameter(take));

                return dialect.QueryMultiple(command, CreateCommit);
            }
        }

        /// <summary>
        /// Get all known stream identifiers.
        /// </summary>
        /// <remarks>This method is not safe to call on an active event store; only use when new streams are not being committed.</remarks>
        public IEnumerable<Guid> GetStreams()
        {
            Verify.NotDisposed(this, disposed);

            return new PagedResult<Guid>(pageSize, (lastResult, page) => GetStreamsAfter(lastResult));
        }

        /// <summary>
        /// Gets paged unique stream identifiers after the specified <paramref name="streamId"/>.
        /// </summary>
        /// <param name="streamId">The last ordered stream identifier from the previous result page.</param>
        private IEnumerable<Guid> GetStreamsAfter(Guid streamId)
        {
            using (var command = dialect.CreateCommand(dialect.GetStreams))
            {
                Log.TraceFormat("Getting next {0} stream identifiers after {1}", pageSize, streamId);

                command.Parameters.Add(dialect.CreateStreamIdParameter(streamId));
                command.Parameters.Add(dialect.CreateTakeParameter(pageSize));

                return dialect.QueryMultiple(command, record => record.GetGuid(0));
            }
        }

        /// <summary>
        /// Gets all commits for the specified <paramref name="streamId"/> with a version greater than or equal to <paramref name="minimumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="minimumVersion">The minimum stream version (inclusive).</param>
        public IEnumerable<Commit> GetStream(Guid streamId, Int32 minimumVersion)
        {
            Verify.NotDisposed(this, disposed);
            Verify.GreaterThan(0, minimumVersion, nameof(minimumVersion));

            return new PagedResult<Commit>(pageSize, (lastResult, page) => GetStreamFrom(streamId, lastResult == null ? minimumVersion : lastResult.Version + 1));
        }

        /// <summary>
        /// Gets paged commits for the specified <paramref name="streamId"/> with a version bounded by <paramref name="minimumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="minimumVersion">The minimum stream version (inclusive).</param>
        private IEnumerable<Commit> GetStreamFrom(Guid streamId, Int32 minimumVersion)
        {
            using (var command = dialect.CreateCommand(dialect.GetStream))
            {
                Log.TraceFormat("Getting next {0} commits for stream {1} from version {2}", pageSize, streamId, minimumVersion);

                command.Parameters.Add(dialect.CreateVersionParameter(minimumVersion));
                command.Parameters.Add(dialect.CreateStreamIdParameter(streamId));
                command.Parameters.Add(dialect.CreateTakeParameter(pageSize));

                return dialect.QueryMultiple(command, CreateCommit);
            }
        }

        /// <summary>
        /// Deletes the specified event stream for <paramref name="streamId"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        public void DeleteStream(Guid streamId)
        {
            Verify.NotDisposed(this, disposed);

            using (var command = dialect.CreateCommand(dialect.DeleteStream))
            {
                Log.TraceFormat("Purging stream {0}", streamId);

                command.Parameters.Add(dialect.CreateStreamIdParameter(streamId));

                dialect.ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Adds a new commit to the event store.
        /// </summary>
        /// <param name="commit">The commit to append to the event store.</param>
        public void Save(Commit commit)
        {
            Verify.NotDisposed(this, disposed);
            Verify.NotNull(commit, nameof(commit));

            var data = serializer.Serialize(new CommitData(commit.Headers, commit.Events));
            using (var command = dialect.CreateCommand(dialect.InsertCommit))
            {
                Log.TraceFormat("Inserting stream {0} commit for version {1}", commit.StreamId, commit.Version);

                command.Parameters.Add(dialect.CreateTimestampParameter(commit.Timestamp));
                command.Parameters.Add(dialect.CreateCorrelationIdParameter(commit.CorrelationId));
                command.Parameters.Add(dialect.CreateStreamIdParameter(commit.StreamId));
                command.Parameters.Add(dialect.CreateVersionParameter(commit.Version));
                command.Parameters.Add(dialect.CreateDataParameter(data));

                commit.Id = Convert.ToInt64(dialect.ExecuteScalar(command));
            }
        }

        /// <summary>
        /// Mark the specified commit as being dispatched.
        /// </summary>
        /// <param name="id">The unique commit identifier that has been dispatched.</param>
        public void MarkDispatched(Int64 id)
        {
            Verify.NotDisposed(this, disposed);

            if (useAsyncWrite)
            {
                dispatchedBuffer.Add(id);
            }
            else
            {
                using (var command = dialect.CreateCommand(dialect.MarkDispatched))
                {
                    Log.TraceFormat("Marking commit {0} as dispatched", id);

                    command.Parameters.Add(dialect.CreateIdParameter(id));

                    dialect.ExecuteNonQuery(command);
                }
            }
        }

        /// <summary>
        /// Migrates the commit <paramref name="headers"/> and <paramref name="events"/> for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique commit identifier.</param>
        /// <param name="headers">The new commit headers.</param>
        /// <param name="events">The new commit events.</param>
        public void Migrate(Int64 id, HeaderCollection headers, EventCollection events)
        {
            Verify.NotDisposed(this, disposed);
            Verify.NotNull(headers, nameof(headers));
            Verify.NotNull(events, nameof(events));

            var data = serializer.Serialize(new CommitData(headers, events));
            using (var command = dialect.CreateCommand(dialect.UpdateCommit))
            {
                Log.TraceFormat("Updating commit {0}", id);

                command.Parameters.Add(dialect.CreateIdParameter(id));
                command.Parameters.Add(dialect.CreateDataParameter(data));

                dialect.ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Deletes all existing commits from the event store.
        /// </summary>
        public void Purge()
        {
            Verify.NotDisposed(this, disposed);

            using (var command = dialect.CreateCommand(dialect.DeleteStreams))
            {
                Log.Trace("Purging event store");

                dialect.ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Creates a new <see cref="Commit"/>.
        /// </summary>
        /// <param name="record">The record from which to create the new <see cref="Commit"/>.</param>
        private Commit CreateCommit(IDataRecord record)
        {
            var id = record.GetInt64(Column.Id);
            var timestamp = record.GetDateTime(Column.Timestamp);
            var correlationId = record.GetGuid(Column.CorrelationId);
            var streamId = record.GetGuid(Column.StreamId);
            var version = record.GetInt32(Column.Version);
            var data = serializer.Deserialize<CommitData>(record.GetBytes(Column.Data));

            return new Commit(id, timestamp, correlationId, streamId, version, data.Headers, data.Events);
        }
    }
}
