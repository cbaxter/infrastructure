using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Spark.Infrastructure.EventStore.Dialects;
using Spark.Infrastructure.Logging;
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

namespace Spark.Infrastructure.EventStore
{
    /// <summary>
    /// An RDBMS event store.
    /// </summary>
    public sealed class DbEventStore : DbStore, IStoreEvents
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IEventStoreDialect dialect;
        private static class Column
        {
            public const Int32 StreamId = 0;
            public const Int32 Version = 1;
            public const Int32 Timestamp = 2;
            public const Int32 CommitId = 3;
            public const Int32 Headers = 4;
            public const Int32 Events = 5;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DbSnapshotStore"/>.
        /// </summary>
        /// <param name="connectionName">The name of the connection string associated with this <see cref="DbSnapshotStore"/>.</param>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> used to store binary data.</param>
        public DbEventStore(String connectionName, ISerializeObjects serializer)
            : this(connectionName, serializer, DialectProvider.GetEventStoreDialect(connectionName))
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="DbSnapshotStore"/> with a custom <see cref="ISnapshotStoreDialect"/>.
        /// </summary>
        /// <param name="connectionName">The name of the connection string associated with this <see cref="DbSnapshotStore"/>.</param>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> used to store binary data.</param>
        /// <param name="dialect">The database dialect associated with the <paramref name="connectionName"/>.</param>
        internal DbEventStore(String connectionName, ISerializeObjects serializer, IEventStoreDialect dialect)
            : base(connectionName, serializer, dialect)
        {
            Verify.NotNull(dialect, "dialect");

            this.dialect = dialect;
        }

        /// <summary>
        /// Initializes a new event store.
        /// </summary>
        public void Initialize()
        {
            using (var command = CreateCommand(dialect.CreateCommitTableStatement))
            {
                Log.TraceFormat("Initializing event store: {0}", command.CommandText);

                ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Gets all commits from the event store.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Commit> GetAll()
        {
            return GetFrom(UnixEpoch);
        }

        /// <summary>
        /// Gets all commits from the event store commited on or after <paramref name="startTime"/>.
        /// </summary>
        /// <param name="startTime">The timestamp of the first commit to be returned (inclusive).</param>
        public IEnumerable<Commit> GetFrom(DateTime startTime)
        {
            return new PagedResult<Commit>(dialect.PageSize, page => GetFrom(startTime, page));
        }

        /// <summary>
        /// Gets paged commits from the event store commited on or after <paramref name="startTime"/>.
        /// </summary>
        /// <param name="startTime">The timestamp of the first commit to be returned (inclusive).</param>
        /// <param name="page">The current page of data to retrieve.</param>
        private IEnumerable<Commit> GetFrom(DateTime startTime, Page page)
        {
            using (var command = new SqlCommand(dialect.GetCommits))
            {
                command.Parameters.Add(dialect.CreateTimestampParameter(startTime));
                command.Parameters.Add(dialect.CreateSkipParameter(page.Skip));
                command.Parameters.Add(dialect.CreateTakeParameter(page.Take));

                return QueryMultiple(command, CreateCommit);
            }
        }

        /// <summary>
        /// Gets all commits for the specified <paramref name="streamId"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        public IEnumerable<Commit> GetStream(Guid streamId)
        {
            return GetStreamFrom(streamId, 0);
        }

        /// <summary>
        /// Gets all commits for the specified <paramref name="streamId"/> with a version greater than or equal to <paramref name="minimumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="minimumVersion">The minimum stream version (inclusive).</param>
        public IEnumerable<Commit> GetStreamFrom(Guid streamId, Int32 minimumVersion)
        {
            return new PagedResult<Commit>(dialect.PageSize, page => GetStreamFrom(streamId, minimumVersion, page));
        }

        /// <summary>
        /// Gets paged commits for the specified <paramref name="streamId"/> with a version greater than or equal to <paramref name="minimumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="minimumVersion">The minimum stream version (inclusive).</param>
        /// <param name="page">The current page of data to retrieve.</param>
        private IEnumerable<Commit> GetStreamFrom(Guid streamId, Int32 minimumVersion, Page page)
        {
            using (var command = new SqlCommand(dialect.GetStream))
            {
                command.Parameters.Add(dialect.CreateStreamIdParameter(streamId));
                command.Parameters.Add(dialect.CreateVersionParameter(minimumVersion));
                command.Parameters.Add(dialect.CreateSkipParameter(page.Skip));
                command.Parameters.Add(dialect.CreateTakeParameter(page.Take));

                return QueryMultiple(command, CreateCommit);
            }
        }

        /// <summary>
        /// Adds a new commit to the event store.
        /// </summary>
        /// <param name="commit">The commit to append to the event store.</param>
        public void SaveCommit(Commit commit)
        {
            using (var command = new SqlCommand(dialect.InsertCommitStatement))
            {
                command.Parameters.Add(dialect.CreateStreamIdParameter(commit.StreamId));
                command.Parameters.Add(dialect.CreateVersionParameter(commit.Version));
                command.Parameters.Add(dialect.CreateTimestampParameter(commit.Timestamp));
                command.Parameters.Add(dialect.CreateCommitIdParameter(commit.CommitId));
                command.Parameters.Add(dialect.CreateHeadersParameter(Serialize(commit.Headers)));
                command.Parameters.Add(dialect.CreateEventsParameter(Serialize(commit.Events)));

                ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Migrates the commit <paramref name="headers"/> and <paramref name="events"/> for the specified <paramref name="commitId"/>.
        /// </summary>
        /// <param name="commitId">The unique commit identifier.</param>
        /// <param name="headers">The new commit headers.</param>
        /// <param name="events">The new commit events.</param>
        public void Migrate(Guid commitId, HeaderCollection headers, EventCollection events)
        {
            using (var command = new SqlCommand(dialect.UpdateCommitStatement))
            {
                command.Parameters.Add(dialect.CreateCommitIdParameter(commitId));
                command.Parameters.Add(dialect.CreateHeadersParameter(Serialize(headers)));
                command.Parameters.Add(dialect.CreateEventsParameter(Serialize(events)));

                ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Deletes the specified event stream for <paramref name="streamId"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        public void Purge(Guid streamId)
        {
            using (var command = new SqlCommand(dialect.DeleteStreamStatement))
            {
                command.Parameters.Add(dialect.CreateStreamIdParameter(streamId));

                ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Deletes all existing commits from the event store.
        /// </summary>
        public void Purge()
        {
            using (var command = new SqlCommand(dialect.DeleteStreamsStatement))
            {

                ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Creates a new <see cref="Commit"/>.
        /// </summary>
        /// <param name="record">The record from which to create the new <see cref="Commit"/>.</param>
        private Commit CreateCommit(IDataRecord record)
        {
            return new Commit(
                record.GetGuid(Column.StreamId),
                record.GetInt32(Column.Version),
                record.GetDateTime(Column.Timestamp),
                record.GetGuid(Column.CommitId),
                new HeaderCollection((IDictionary<String, Object>)Deserialize(record, Column.Headers)),
                new EventCollection((IList<Object>)Deserialize(record, Column.Events))
            );
        }
    }
}
