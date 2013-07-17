using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Spark.Infrastructure.Resources;

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

namespace Spark.Infrastructure.EventStore.Sql.Dialects
{
    internal sealed class SqlServerDialect : IEventStoreDialect, ISnapshotStoreDialect
    {
        private const Int32 Max = -1;
        private const Int32 UniqueIndexViolation = 2601;
        private const Int32 UniqueConstraintViolation = 2627;
        private readonly String connectionString;

        public SqlServerDialect(String connectionName)
        {
            Verify.NotNullOrWhiteSpace(connectionName, "connectionName");

            connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
        }

        // Database Provider
        public DbProviderFactory Provider { get { return SqlClientFactory.Instance; } }
        public String ConnectionString { get { return connectionString; } }

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

        // ISnapshotStoreDialect
        public String GetSnapshot { get { return SqlServerDialectStatements.GetSnapshot; } }
        public String InsertSnapshot { get { return SqlServerDialectStatements.InsertSnapshot; } }
        public String ReplaceSnapshot { get { return SqlServerDialectStatements.ReplaceSnapshot; } }
        public String DeleteSnapshots { get { return SqlServerDialectStatements.PurgeSnapshots; } }
        public String EnsureSnapshotTableExists { get { return SqlServerDialectStatements.EnsureSnapshotTableExists; } }

        // Create Methods
        public DbParameter CreateIdParameter(Int64 id) { return new SqlParameter("@id", SqlDbType.BigInt) { SourceColumn = "id", Value = id }; }
        public DbParameter CreateTimestampParameter(DateTime timestamp) { return new SqlParameter("@timestamp", SqlDbType.DateTime2) { SourceColumn = "timestamp", Value = timestamp }; }
        public DbParameter CreateCorrelationIdParameter(Guid correlationId) { return new SqlParameter("@correlationId", SqlDbType.UniqueIdentifier) { SourceColumn = "correlationId", Value = correlationId }; }
        public DbParameter CreateStreamIdParameter(Guid streamId) { return new SqlParameter("@streamId", SqlDbType.UniqueIdentifier) { SourceColumn = "streamId", Value = streamId }; }
        public DbParameter CreateVersionParameter(Int32 version) { return new SqlParameter("@version", SqlDbType.Int) { SourceColumn = "version", Value = version }; }
        public DbParameter CreateHeadersParameter(Byte[] headers) { return new SqlParameter("@headers", SqlDbType.VarBinary, Max) { SourceColumn = "headers", Value = headers }; }
        public DbParameter CreateEventsParameter(Byte[] events) { return new SqlParameter("@events", SqlDbType.VarBinary, Max) { SourceColumn = "events", Value = events }; }
        public DbParameter CreateDataParameter(Byte[] data) { return new SqlParameter("@data", SqlDbType.VarBinary, Max) { SourceColumn = "data", Value = data }; }
        public DbParameter CreateStateParameter(Byte[] state) { return new SqlParameter("@state", SqlDbType.VarBinary, Max) { SourceColumn = "state", Value = state }; }
        public DbParameter CreateSkipParameter(Int64 skip) { return new SqlParameter("@skip", SqlDbType.BigInt) { SourceColumn = "skip", Value = skip }; }
        public DbParameter CreateTakeParameter(Int64 take) { return new SqlParameter("@take", SqlDbType.BigInt) { SourceColumn = "take", Value = take }; }

        // Translate
        public Exception Translate(DbCommand command, DbException ex)
        {
            var sqlException = ex as SqlException;
            if (sqlException == null)
                return ex;

            if (sqlException.Number == UniqueIndexViolation)
            {
                var commitId = command.GetParameterValue("@id");

                return new DuplicateCommitException(Exceptions.DuplicateCommitException.FormatWith(commitId));
            }

            if (sqlException.Number == UniqueConstraintViolation)
            {
                var streamId = command.GetParameterValue("@streamId");
                var version = command.GetParameterValue("@version");

                return new ConcurrencyException(Exceptions.ConcurrencyException.FormatWith(streamId, version));
            }

            return ex;
        }
    }
}
