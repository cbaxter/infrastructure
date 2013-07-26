using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Spark.Data;
using Spark.Resources;

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

namespace Spark.EventStore.Sql.Dialects
{
    /// <summary>
    /// The base SQL RDBMS dialect.
    /// </summary>
    public abstract class SqlDialect : IDbDialect
    {
        protected const Int32 Max = -1;
        private readonly String connectionString;

        public DbProviderFactory Provider { get { return SqlClientFactory.Instance; } }
        public String ConnectionString { get { return connectionString; } }

        internal SqlDialect(String connectionName)
        {
            Verify.NotNullOrWhiteSpace(connectionName, "connectionName");

            connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
        }

        public Exception Translate(IDbCommand command, DbException ex)
        {
            var sqlException = ex as SqlException;
            if (sqlException == null)
                return ex;
            
            const Int32 uniqueIndexViolation = 2601;
            if (sqlException.Number == uniqueIndexViolation)
            {
                var commitId = command.GetParameterValue("@id");

                return new DuplicateCommitException(Exceptions.DuplicateCommitException.FormatWith(commitId));
            }
            
            const Int32 uniqueConstraintViolation = 2627;
            if (sqlException.Number == uniqueConstraintViolation)
            {
                var streamId = command.GetParameterValue("@streamId");
                var version = command.GetParameterValue("@version");

                return new ConcurrencyException(Exceptions.ConcurrencyException.FormatWith(streamId, version));
            }

            return ex;
        }
    }

    /// <summary>
    /// The SQL RDBMS dialect statements associated with an <see cref="SqlEventStore"/> instance.
    /// </summary>
    public sealed class SqlEventStoreDialect : SqlDialect, IEventStoreDialect
    {
        public SqlEventStoreDialect() : base("eventStore") { }
        public SqlEventStoreDialect(String connectionName) : base(connectionName) { }

        // IEventStoreDialect
        public String GetRange { get { return SqlServerDialectStatements.GetRange; } }
        public String GetStream { get { return SqlServerDialectStatements.GetStream; } }
        public String GetStreams { get { return SqlServerDialectStatements.GetStreams; } }
        public String GetUndispatched { get { return SqlServerDialectStatements.GetUndispatched; } }
        public String MarkDispatched { get { return SqlServerDialectStatements.MarkDispatched; } }
        public String InsertCommit { get { return SqlServerDialectStatements.InsertCommit; } }
        public String UpdateCommit { get { return SqlServerDialectStatements.UpdateCommit; } }
        public String DeleteStream { get { return SqlServerDialectStatements.DeleteStream; } }
        public String DeleteStreams { get { return SqlServerDialectStatements.PurgeCommits; } }

        public String EnsureCommitTableExists { get { return SqlServerDialectStatements.EnsureCommitTableExists; } }
        public String EnsureDuplicateCommitsDetected { get { return SqlServerDialectStatements.EnsureDuplicateCommitsDetected; } }
        public String EnsureDuplicateCommitsSuppressed { get { return SqlServerDialectStatements.EnsureDuplicateCommitsSuppressed; } }

        // Create Methods
        public IDataParameter CreateIdParameter(Int64 id) { return new SqlParameter("@id", SqlDbType.BigInt) { SourceColumn = "id", Value = id }; }
        public IDataParameter CreateTimestampParameter(DateTime timestamp) { return new SqlParameter("@timestamp", SqlDbType.DateTime2) { SourceColumn = "timestamp", Value = timestamp }; }
        public IDataParameter CreateCorrelationIdParameter(Guid correlationId) { return new SqlParameter("@correlationId", SqlDbType.UniqueIdentifier) { SourceColumn = "correlationId", Value = correlationId }; }
        public IDataParameter CreateStreamIdParameter(Guid streamId) { return new SqlParameter("@streamId", SqlDbType.UniqueIdentifier) { SourceColumn = "streamId", Value = streamId }; }
        public IDataParameter CreateVersionParameter(Int32 version) { return new SqlParameter("@version", SqlDbType.Int) { SourceColumn = "version", Value = version }; }
        public IDataParameter CreateDataParameter(Byte[] data) { return new SqlParameter("@data", SqlDbType.VarBinary, Max) { SourceColumn = "data", Value = data }; }
        public IDataParameter CreateSkipParameter(Int64 skip) { return new SqlParameter("@skip", SqlDbType.BigInt) { SourceColumn = "skip", Value = skip }; }
        public IDataParameter CreateTakeParameter(Int64 take) { return new SqlParameter("@take", SqlDbType.BigInt) { SourceColumn = "take", Value = take }; }
    }
    
    /// <summary>
    /// The SQL RDBMS dialect statements associated with a <see cref="SqlSnapshotStore"/> instance.
    /// </summary>
    public sealed class SqlSnapshotStoreDialect : SqlDialect, ISnapshotStoreDialect
    {
        public SqlSnapshotStoreDialect() : base("snapshotStore") { }
        public SqlSnapshotStoreDialect(String connectionName) : base(connectionName) { }

        // ISnapshotStoreDialect
        public String GetSnapshot { get { return SqlServerDialectStatements.GetSnapshot; } }
        public String InsertSnapshot { get { return SqlServerDialectStatements.InsertSnapshot; } }
        public String ReplaceSnapshot { get { return SqlServerDialectStatements.ReplaceSnapshot; } }
        public String DeleteSnapshots { get { return SqlServerDialectStatements.PurgeSnapshots; } }
        public String EnsureSnapshotTableExists { get { return SqlServerDialectStatements.EnsureSnapshotTableExists; } }

        // Create Methods
        public IDataParameter CreateStreamIdParameter(Guid streamId) { return new SqlParameter("@streamId", SqlDbType.UniqueIdentifier) { SourceColumn = "streamId", Value = streamId }; }
        public IDataParameter CreateVersionParameter(Int32 version) { return new SqlParameter("@version", SqlDbType.Int) { SourceColumn = "version", Value = version }; }
        public IDataParameter CreateStateParameter(Byte[] state) { return new SqlParameter("@state", SqlDbType.VarBinary, Max) { SourceColumn = "state", Value = state }; }
    }
}
