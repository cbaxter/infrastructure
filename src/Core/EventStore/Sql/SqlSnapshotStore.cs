using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.Logging;
using Spark.Infrastructure.Resources;
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
    /// An RDBMS snapshot store.
    /// </summary>
    public sealed class SqlSnapshotStore : IStoreSnapshots, IDisposable
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly ISnapshotStoreDialect dialect;
        private readonly ISerializeObjects serializer;
        private readonly EventWaitHandle waitHandle;
        private readonly Boolean replaceExisting;
        private readonly Thread backgroundWorker;
        private readonly TimeSpan flushInterval;
        private readonly Boolean useAsyncWrite;
        private readonly DataTable buffer;
        private readonly Int32 batchSize;
        private Boolean disposed;

        private static class Column
        {
            public const Int32 StreamId = 0;
            public const Int32 Version = 1;
            public const Int32 State = 2;
        }

        /// <summary>
        /// Returns <value>true</value> if the current batch is less than <see cref="batchSize"/>; otherwise returns <value>false</value>.
        /// </summary>
        private Boolean WaitForMoreSnapshots { get { lock (buffer) { return buffer.Rows.Count < batchSize; } } }

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
            this.batchSize = settings.BatchSize;
            this.flushInterval = settings.FlushInterval;
            this.replaceExisting = settings.ReplaceExisting;
            this.waitHandle = new ManualResetEvent(initialState: false);
            this.backgroundWorker = new Thread(WaitForSnapshots) { Name = "Snapshot Writer", IsBackground = true };
            this.buffer = CreateSnapshotBuffer();

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
            {
                waitHandle.Set();
                backgroundWorker.Join();
            }

            waitHandle.Dispose();
            buffer.Dispose();
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

            if (useAsyncWrite)
                backgroundWorker.Start();
        }

        /// <summary>
        /// Creates a <see cref="DataTable"/> based on the required insert/update command parameters.
        /// </summary>
        private DataTable CreateSnapshotBuffer()
        {
            var buffer = new DataTable("Snapshot");

            buffer.Columns.Add(GetColumnName(dialect.CreateStreamIdParameter(Guid.Empty)), typeof(Guid));
            buffer.Columns.Add(GetColumnName(dialect.CreateVersionParameter(0)), typeof(Int32));
            buffer.Columns.Add(GetColumnName(dialect.CreateStateParameter(null)), typeof(Byte[]));

            return buffer;
        }

        /// <summary>
        /// Gets the column name for the specified <paramref name="parameter"/> (source column must be set).
        /// </summary>
        /// <param name="parameter">The parameter for which the source column is to be retrieved.</param>
        private static String GetColumnName(DbParameter parameter)
        {
            Verify.NotNull(parameter, "parameter");

            if (parameter.SourceColumn.IsNullOrWhiteSpace())
                throw new MappingException(Exceptions.ParameterSourceColumnNotSet.FormatWith(parameter.ParameterName));

            return parameter.SourceColumn;
        }

        /// <summary>
        /// Gets the most recent snapshot for the specified <paramref name="streamId"/> and <paramref name="maximumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="maximumVersion">The maximum snapshot version.</param>
        public Snapshot GetSnapshot(Guid streamId, Int32 maximumVersion)
        {
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
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (useAsyncWrite)
            {
                BufferSnapshot(snapshot);
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
        private void BufferSnapshot(Snapshot snapshot)
        {
            var state = serializer.Serialize(snapshot.State);

            lock (buffer)
            {
                buffer.Rows.Add(snapshot.StreamId, snapshot.Version, state);
            }

            waitHandle.Set();
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

        /// <summary>
        /// Wait for one or more snapshots to write out to the underlying data store.
        /// </summary>
        private void WaitForSnapshots()
        {
            try
            {
                while (!disposed && waitHandle.WaitOne())
                {
                    waitHandle.Reset();

                    if (!disposed && WaitForMoreSnapshots)
                        Thread.Sleep(flushInterval);

                    WriteBufferToDataStore();
                }

                // WaitOne returns true when signal received and false if the wait has timed out; using Timeout.Infinity this statement should only be reached on a clean shutdown.
                if (!disposed)
                    Log.Warn("Background worker aborted.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        /// <summary>
        /// Write out the current batch of snapshots to the underlying data store.
        /// </summary>
        private void WriteBufferToDataStore()
        {
            try
            {
                var batch = CopyAndClearBuffer();
                if (batch.Rows.Count == 0)
                    return;

                using (var connection = dialect.OpenConnection())
                using (var dataAdapter = dialect.Provider.CreateDataAdapter())
                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                using (var command = dialect.CreateCommand(replaceExisting ? dialect.ReplaceSnapshot : dialect.InsertSnapshot))
                {
                    Debug.Assert(dataAdapter != null);

                    dataAdapter.ContinueUpdateOnError = true;
                    dataAdapter.UpdateBatchSize = batchSize;
                    dataAdapter.InsertCommand = command;

                    command.Connection = connection;
                    command.Transaction = transaction;
                    command.UpdatedRowSource = UpdateRowSource.None;
                    command.Parameters.Add(dialect.CreateStreamIdParameter(Guid.Empty));
                    command.Parameters.Add(dialect.CreateVersionParameter(0));
                    command.Parameters.Add(dialect.CreateStateParameter(null));

                    dataAdapter.Update(batch);

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                // Snapshots are non-critical and highly unlikely to fail (short of network/database connectivity issues); just log the failure and continue.
                Log.Error(ex.Message);
            }
        }

        /// <summary>
        /// Copies the currently buffered snapshots to a new data table and clears the buffer to prepare for the next batch.
        /// </summary>
        private DataTable CopyAndClearBuffer()
        {
            lock (buffer)
            {
                var copy = buffer.Copy();

                buffer.Clear();

                return copy;
            }
        }
    }
}
