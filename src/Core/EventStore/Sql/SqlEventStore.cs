using System;
using System.Collections.Generic;
using System.Data;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Logging;
using Spark.Infrastructure.Messaging;
using Spark.Infrastructure.Serialization;

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

namespace Spark.Infrastructure.EventStore.Sql
{
    /// <summary>
    /// An RDBMS event store.
    /// </summary>
    public sealed class SqlEventStore : SqlStore, IStoreEvents
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly Boolean detectDuplicateCommits;
        private readonly IEventStoreDialect dialect;
        private readonly Int64 pageSize;

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
        /// <param name="connectionName">The name of the connection string associated with this <see cref="SqlEventStore"/>.</param>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> used to store binary data.</param>
        public SqlEventStore(String connectionName, ISerializeObjects serializer)
            : this(connectionName, serializer, Settings.Eventstore, DialectProvider.GetEventStoreDialect(connectionName))
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="SqlEventStore"/> with a custom <see cref="IEventStoreDialect"/>.
        /// </summary>
        /// <param name="connectionName">The name of the connection string associated with this <see cref="SqlEventStore"/>.</param>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> used to store binary data.</param>
        /// <param name="settings">The event store settings.</param>
        /// <param name="dialect">The database dialect associated with the <paramref name="connectionName"/>.</param>
        internal SqlEventStore(String connectionName, ISerializeObjects serializer, IStoreEventSettings settings, IEventStoreDialect dialect)
            : base(connectionName, serializer, dialect)
        {
            Verify.NotNull(dialect, "dialect");
            Verify.NotNull(settings, "settings");

            this.dialect = dialect;
            this.pageSize = settings.PageSize;
            this.detectDuplicateCommits = settings.DetectDuplicateCommits;

            Initialize();
        }

        /// <summary>
        /// Initializes a new event store.
        /// </summary>
        private void Initialize()
        {
            Log.Trace("Initializing event store");

            using (var command = CreateCommand(dialect.EnsureCommitTableExists))
                ExecuteNonQuery(command);

            // NOTE: Most durable message queues ensure `at least once` delivery of messages; however when using an internal message 
            //       queue (i.e., BlockingCollection) or non-durable message queue (i.e., IPC via named pipes) duplicate commits may 
            //       not need to be enforced. Allow additional commit ID unique index be created/droped as desired.
            using (var command = CreateCommand(detectDuplicateCommits ? dialect.EnsureDuplicateCommitsDetected : dialect.EnsureDuplicateCommitsSuppressed))
                ExecuteNonQuery(command);
        }

        /// <summary>
        /// Get the specified commit sequence range starting after <paramref name="skip"/> and returing <paramref name="take"/> commits.
        /// </summary>
        /// <param name="skip">The commit sequence lower bound (exclusive).</param>
        /// <param name="take">The number of commits to include in the result.</param>
        public IReadOnlyList<Commit> GetRange(Int64 skip, Int64 take)
        {
            Verify.GreaterThanOrEqual(0, skip, "skip");
            Verify.GreaterThan(0, take, "take");

            using (var command = CreateCommand(dialect.GetRange))
            {
                Log.TraceFormat("Getting next {0} commits after {1}", take, skip);

                command.Parameters.Add(dialect.CreateSkipParameter(skip + 1));
                command.Parameters.Add(dialect.CreateTakeParameter(take));

                return QueryMultiple(command, CreateCommit);
            }
        }

        /// <summary>
        /// Get all known stream identifiers.
        /// </summary>
        /// <remarks>This method is not safe to call on an active event store; only use when new streams are not being committed.</remarks>
        public IEnumerable<Guid> GetStreams()
        {
            return new PagedResult<Guid>(pageSize, (lastResult, page) => GetStreamsAfter(lastResult));
        }

        /// <summary>
        /// Gets paged unique stream identifiers after the specified <paramref name="streamId"/>.
        /// </summary>
        /// <param name="streamId">The last ordered stream identifier from the previous result page.</param>
        private IEnumerable<Guid> GetStreamsAfter(Guid streamId)
        {
            using (var command = CreateCommand(dialect.GetStreams))
            {
                Log.TraceFormat("Getting next {0} stream identifiers after {1}", pageSize, streamId);

                command.Parameters.Add(dialect.CreateStreamIdParameter(streamId));
                command.Parameters.Add(dialect.CreateTakeParameter(pageSize));

                return QueryMultiple(command, record => record.GetGuid(0));
            }
        }

        /// <summary>
        /// Gets all commits for the specified <paramref name="streamId"/> with a version greater than or equal to <paramref name="minimumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="minimumVersion">The minimum stream version (inclusive).</param>
        public IEnumerable<Commit> GetStream(Guid streamId, Int32 minimumVersion)
        {
            Verify.GreaterThan(0, minimumVersion, "minimumVersion");

            return new PagedResult<Commit>(pageSize, (lastResult, page) => GetStreamFrom(streamId, lastResult == null ? minimumVersion : lastResult.Version + 1));
        }

        /// <summary>
        /// Gets paged commits for the specified <paramref name="streamId"/> with a version bounded by <paramref name="minimumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="minimumVersion">The minimum stream version (inclusive).</param>
        private IEnumerable<Commit> GetStreamFrom(Guid streamId, Int32 minimumVersion)
        {
            using (var command = CreateCommand(dialect.GetStream))
            {
                Log.TraceFormat("Getting next {0} commits for stream {1} from version {2}", pageSize, streamId, minimumVersion);

                command.Parameters.Add(dialect.CreateVersionParameter(minimumVersion));
                command.Parameters.Add(dialect.CreateStreamIdParameter(streamId));
                command.Parameters.Add(dialect.CreateTakeParameter(pageSize));

                return QueryMultiple(command, CreateCommit);
            }
        }

        /// <summary>
        /// Deletes the specified event stream for <paramref name="streamId"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        public void DeleteStream(Guid streamId)
        {
            using (var command = CreateCommand(dialect.DeleteStream))
            {
                Log.TraceFormat("Purging stream {0}", streamId);

                command.Parameters.Add(dialect.CreateStreamIdParameter(streamId));

                ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Adds a new commit to the event store.
        /// </summary>
        /// <param name="commit">The commit to append to the event store.</param>
        public void Save(Commit commit)
        {
            Verify.NotNull(commit, "commit");

            var data = Serialize(new CommitData(commit.Headers, commit.Events));
            using (var command = CreateCommand(dialect.InsertCommit))
            {
                Log.TraceFormat("Inserting stream {0} commit for version {1}", commit.StreamId, commit.Version);

                command.Parameters.Add(dialect.CreateTimestampParameter(commit.Timestamp));
                command.Parameters.Add(dialect.CreateCorrelationIdParameter(commit.CorrelationId));
                command.Parameters.Add(dialect.CreateStreamIdParameter(commit.StreamId));
                command.Parameters.Add(dialect.CreateVersionParameter(commit.Version));
                command.Parameters.Add(dialect.CreateDataParameter(data));

                commit.Id = Convert.ToInt64(ExecuteScalar(command));
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
            Verify.NotNull(headers, "headers");
            Verify.NotNull(events, "events");

            var data = Serialize(new CommitData(headers, events));
            using (var command = CreateCommand(dialect.UpdateCommit))
            {
                Log.TraceFormat("Updating commit {0}", id);

                command.Parameters.Add(dialect.CreateIdParameter(id));
                command.Parameters.Add(dialect.CreateDataParameter(data));

                ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Deletes all existing commits from the event store.
        /// </summary>
        public void Purge()
        {
            using (var command = CreateCommand(dialect.DeleteStreams))
            {
                Log.Trace("Purging event store");

                ExecuteNonQuery(command);
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
            var data = Deserialize<CommitData>(record.GetBytes(Column.Data));

            return new Commit(id, timestamp, correlationId, streamId, version, data.Headers, data.Events);
        }
    }
}
