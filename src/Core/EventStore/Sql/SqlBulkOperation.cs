using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using Spark.Cqrs.Domain;
using Spark.Logging;
using Spark.Resources;

namespace Spark.EventStore.Sql
{
    /// <summary>
    /// An auto flushing async buffered batch command executor.
    /// </summary>
    internal sealed class SqlBatchOperation : IDisposable
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly EventWaitHandle waitHandle;
        private readonly DbCommand commandTemplate;
        private readonly Thread backgroundWorker;
        private readonly TimeSpan flushInterval;
        private readonly ISqlDialect dialect;
        private readonly DataTable buffer;
        private readonly Int32 batchSize;
        private Boolean autoFlush;
        private Boolean disposed;

        /// <summary>
        /// Returns <value>true</value> if the current batch is less than <see cref="batchSize"/>; otherwise returns <value>false</value>.
        /// </summary>
        private Boolean WaitForMoreData { get { lock (buffer) { return buffer.Rows.Count < batchSize; } } }

        /// <summary>
        /// Gets or sets whether the batch operation will be flushed asynchronously (<value>true</value>) or explicit use of <see cref="Flush"/> is required (<value>false</value>).
        /// </summary>
        public Boolean AutoFlush { get { return autoFlush; } set { autoFlush = value; if (value) { waitHandle.Set(); } } }

        /// <summary>
        /// Initializes a new instance of <see cref="SqlBatchOperation"/>.
        /// </summary>
        /// <param name="commandTemplate">The <see cref="DbCommand"/> template on which the <see cref="SqlBatchOperation"/> is based.</param>
        /// <param name="dialect">The <see cref="ISqlDialect"/> associated with this <see cref="SqlBatchOperation"/>.</param>
        /// <param name="batchSize">The maximum number of items to be written in a batch.</param>
        /// <param name="flushInterval">The frequency with which the current batch is to be flushed.</param>
        public SqlBatchOperation(ISqlDialect dialect, DbCommand commandTemplate, Int32 batchSize, TimeSpan flushInterval)
        {
            this.autoFlush = true;
            this.dialect = dialect;
            this.batchSize = batchSize;
            this.flushInterval = flushInterval;
            this.commandTemplate = commandTemplate.Clone();
            this.waitHandle = new ManualResetEvent(initialState: false);
            this.backgroundWorker = new Thread(WaitForData) { IsBackground = true };
            this.buffer = CreateBuffer(commandTemplate);
            this.backgroundWorker.Start();
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="SqlBatchOperation"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            waitHandle.Set();
            backgroundWorker.Join();
            waitHandle.Dispose();
            buffer.Dispose();
        }

        /// <summary>
        /// Creates a <see cref="DataTable"/> based on the <paramref name="commandTemplate"/>  parameters.
        /// </summary>
        private static DataTable CreateBuffer(DbCommand commandTemplate)
        {
            var buffer = new DataTable();

            foreach (DbParameter parameter in commandTemplate.Parameters)
                buffer.Columns.Add(GetColumnName(parameter), typeof(Object));

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
        /// Adds a new record to the underlying batch.
        /// </summary>
        /// <param name="values">The set of values representing <see cref="DbParameter"/> values for a new batch item.</param>
        /// <remarks>Value indices should map to the original command template parameter indicies.</remarks>
        public void Add(params Object[] values)
        {
            Verify.NotDisposed(this, disposed);

            lock (buffer)
            {
                buffer.Rows.Add(values);
            }

            if (autoFlush)
                waitHandle.Set();
        }

        /// <summary>
        /// Flush any records currently buffered to the underlying data store.
        /// </summary>
        public void Flush()
        {
            Verify.NotDisposed(this, disposed);

            WriteBufferToDataStore();
        }

        /// <summary>
        /// Wait for one or more records before writing out to the underlying data store.
        /// </summary>
        private void WaitForData()
        {
            try
            {
                while (!disposed && waitHandle.WaitOne())
                {
                    waitHandle.Reset();

                    if (!disposed && WaitForMoreData)
                        Thread.Sleep(flushInterval);

                    WriteBufferToDataStoreSafe();
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
        /// Write out the current batch to the underlying data store logging any thrown exceptions.
        /// </summary>
        private void WriteBufferToDataStoreSafe()
        {
            try
            {
                WriteBufferToDataStore();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        /// <summary>
        /// Write out the current batch to the underlying data store.
        /// </summary>
        private void WriteBufferToDataStore()
        {
            using (var batch = CopyAndClearBuffer())
            using (var command = commandTemplate.Clone())
            using (var connection = dialect.OpenConnection())
            using (var dataAdapter = dialect.Provider.CreateDataAdapter())
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                Debug.Assert(dataAdapter != null);

                command.Connection = connection;
                command.Transaction = transaction;
                command.UpdatedRowSource = UpdateRowSource.None;

                dataAdapter.ContinueUpdateOnError = true;
                dataAdapter.UpdateBatchSize = batchSize;
                dataAdapter.InsertCommand = command;
                dataAdapter.Update(batch);

                transaction.Commit();
            }
        }

        /// <summary>
        /// Copies the currently buffered data to a new data table and clears the buffer to prepare for the next batch.
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
