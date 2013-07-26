using System;
using System.Data;
using Spark.Configuration;
using Spark.Logging;
using Spark.Serialization;

/* Copyright (c) 2013 Spark Software Ltd.
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

namespace Spark.EventStore.Sql
{
    /// <summary>
    /// An RDBMS snapshot store.
    /// </summary>
    public sealed class SqlSnapshotStore : IStoreSnapshots, IDisposable
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly ISnapshotStoreDialect dialect;
        private readonly ISerializeObjects serializer;
        private readonly SqlBatchOperation buffer;
        private readonly Boolean replaceExisting;
        private readonly Boolean useAsyncWrite;
        private Boolean disposed;

        private static class Column
        {
            public const Int32 StreamId = 0;
            public const Int32 Version = 1;
            public const Int32 State = 2;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SqlSnapshotStore"/>.
        /// </summary>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> used to store binary data.</param>
        /// <param name="connectionName">The name of the connection string associated with this <see cref="SqlSnapshotStore"/>.</param>
        public SqlSnapshotStore(ISerializeObjects serializer, String connectionName)
            : this(serializer, Settings.SnapshotStore, DialectProvider.GetSnapshotStoreDialect(connectionName))
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="SqlSnapshotStore"/> with a custom <see cref="ISnapshotStoreDialect"/>.
        /// </summary>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> used to store binary data.</param>
        /// <param name="settings">The snapshot store settings.</param>
        /// <param name="dialect">The database dialect associated with this <see cref="SqlSnapshotStore"/>.</param>
        internal SqlSnapshotStore(ISerializeObjects serializer, IStoreSnapshotSettings settings, ISnapshotStoreDialect dialect)
        {
            Verify.NotNull(serializer, "serializer");
            Verify.NotNull(settings, "settings");
            Verify.NotNull(dialect, "dialect");

            this.dialect = dialect;
            this.serializer = serializer;
            this.useAsyncWrite = settings.Async;
            this.replaceExisting = settings.ReplaceExisting;
            this.buffer = settings.Async ? CreateBuffer(settings, dialect) : null;

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
                buffer.Dispose();
        }

        /// <summary>
        /// Creates a <see cref="DataTable"/> based on the required insert/update command parameters.
        /// </summary>
        private static SqlBatchOperation CreateBuffer(IStoreSnapshotSettings settings, ISnapshotStoreDialect dialect)
        {
            using (var command = dialect.CreateCommand(settings.ReplaceExisting ? dialect.ReplaceSnapshot : dialect.InsertSnapshot))
            {
                command.Parameters.Add(dialect.CreateStreamIdParameter(default(Guid)));
                command.Parameters.Add(dialect.CreateVersionParameter(default(Int32)));
                command.Parameters.Add(dialect.CreateStateParameter(default(Byte[])));

                return new SqlBatchOperation(dialect, command, settings.BatchSize, settings.FlushInterval);
            }
        }

        /// <summary>
        /// Initializes a new snapshot store.
        /// </summary>
        private void Initialize()
        {
            using (var command = dialect.CreateCommand(dialect.EnsureSnapshotTableExists))
            {
                Log.Trace("Initializing snapshot store");

                dialect.ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Gets the most recent snapshot for the specified <paramref name="streamId"/> and <paramref name="maximumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="maximumVersion">The maximum snapshot version.</param>
        public Snapshot GetSnapshot(Guid streamId, Int32 maximumVersion)
        {
            Verify.NotDisposed(this, disposed);

            using (var command = dialect.CreateCommand(dialect.GetSnapshot))
            {
                Log.TraceFormat("Getting stream {0} snapshot with version less than or equal to {1}", streamId, maximumVersion);

                command.Parameters.Add(dialect.CreateStreamIdParameter(streamId));
                command.Parameters.Add(dialect.CreateVersionParameter(maximumVersion));

                return dialect.QuerySingle(command, CreateSnapshot);
            }
        }

        /// <summary>
        /// Adds a new <see cref="Snapshot"/> to the snapshot store.
        /// </summary>
        /// <param name="snapshot">The snapshot to save to the snapshot store.</param>
        public void Save(Snapshot snapshot)
        {
            Verify.NotDisposed(this, disposed);

            if (useAsyncWrite)
            {
                buffer.Add(snapshot.StreamId, snapshot.Version, serializer.Serialize(snapshot.State));
            }
            else
            {
                if (replaceExisting)
                    UpdateSnapshot(snapshot);
                else
                    InsertSnapshot(snapshot);
            }
        }

        /// <summary>
        /// Adds a new snapshot to the snapshot store, keeping all existing snapshots.
        /// </summary>
        /// <param name="snapshot">The snapshot to append to the snapshot store.</param>
        private void InsertSnapshot(Snapshot snapshot)
        {
            var state = serializer.Serialize(snapshot.State);

            using (var command = dialect.CreateCommand(dialect.InsertSnapshot))
            {
                Log.TraceFormat("Inserting stream {0} snapshot for version {1}", snapshot.StreamId, snapshot.Version);

                command.Parameters.Add(dialect.CreateStreamIdParameter(snapshot.StreamId));
                command.Parameters.Add(dialect.CreateVersionParameter(snapshot.Version));
                command.Parameters.Add(dialect.CreateStateParameter(state));

                dialect.ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Replaces any existing snapshot with the specified <paramref name="snapshot"/>.
        /// </summary>
        /// <param name="snapshot">The snapshot to replace any existing snapshot.</param>
        private void UpdateSnapshot(Snapshot snapshot)
        {
            var state = serializer.Serialize(snapshot.State);

            using (var command = dialect.CreateCommand(dialect.ReplaceSnapshot))
            {
                Log.TraceFormat("Updating stream {0} snapshot to version {1}", snapshot.StreamId, snapshot.Version);

                command.Parameters.Add(dialect.CreateStreamIdParameter(snapshot.StreamId));
                command.Parameters.Add(dialect.CreateVersionParameter(snapshot.Version));
                command.Parameters.Add(dialect.CreateStateParameter(state));

                dialect.ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Deletes all existing snapshots from the snapshot store.
        /// </summary>
        public void Purge()
        {
            Verify.NotDisposed(this, disposed);

            using (var command = dialect.CreateCommand(dialect.DeleteSnapshots))
            {
                Log.Trace("Purging snapshot store");

                dialect.ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Creates a new <see cref="Snapshot"/>.
        /// </summary>
        /// <param name="record">The record from which to create the new <see cref="Snapshot"/>.</param>
        private Snapshot CreateSnapshot(IDataRecord record)
        {
            return new Snapshot(record.GetGuid(Column.StreamId), record.GetInt32(Column.Version), serializer.Deserialize<Object>(record.GetBytes(Column.State)));
        }
    }
}
