using System;
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

namespace Spark.Infrastructure.EventStore.Dialects
{
    /// <summary>
    /// Microsoft SQL-Server Dialect.
    /// </summary>
    internal sealed class SqlServerDialect : IEventStoreDialect, ISnapshotStoreDialect
    {
        public const Int32 DefaultPageSize = 100;
        public const Int32 UniqueIndexViolation = 2601;
        public const Int32 UniqueConstraintViolation = 2627;
        private readonly Int32 pageSize;

        // IEventStoreDialect
        public Int32 PageSize { get { return pageSize; } }
        public String GetStream { get { return SqlServerSqlStatements.GetStreamFrom; } }
        public String GetCommits { get { return SqlServerSqlStatements.GetCommitsFrom; } }
        public String InsertCommitStatement { get { return SqlServerSqlStatements.InsertCommit; } }
        public String UpdateCommitStatement { get { return SqlServerSqlStatements.UpdateCommit; } }
        public String DeleteStreamStatement { get { return SqlServerSqlStatements.DeleteStream; } }
        public String DeleteStreamsStatement { get { return SqlServerSqlStatements.PurgeCommits; } }
        public String EnsureCommitTableCreatedStatement { get { return SqlServerSqlStatements.EnsureCommitTableCreated; } }
        public String EnsureDuplicateCommitsSuppressedStatement { get { return SqlServerSqlStatements.EnsureDuplicateCommitsSuppressed; } }
        public String EnsureDuplicateCommitsDetectedStatement { get { return SqlServerSqlStatements.EnsureDuplicateCommitsDetected; } }
        public String EnsureTimestampIndexCreatedStatement { get { return SqlServerSqlStatements.EnsureTimestampIndexCreated; } }
        public String EnsureTimestampIndexDroppedStatement { get { return SqlServerSqlStatements.EnsureTimestampIndexDropped; } }

        // ISnapshotStoreDialect
        public String GetSnapshotStatement { get { return SqlServerSqlStatements.GetSnapshot; } }
        public String InsertSnapshotStatement { get { return SqlServerSqlStatements.InsertSnapshot; } }
        public String ReplaceSnapshotStatement { get { return SqlServerSqlStatements.ReplaceSnapshot; } }
        public String DeleteSnapshotsStatement { get { return SqlServerSqlStatements.PurgeSnapshots; } }
        public String EnsureSnapshotTableCreatedStatement { get { return SqlServerSqlStatements.EnsureSnapshotTableCreated; } }

        /// <summary>
        /// Initializes a default instance of <see cref="SqlServerDialect"/>.
        /// </summary>
        public SqlServerDialect()
            : this(DefaultPageSize)
        { }

        /// <summary>
        /// Initializes an instance of <see cref="SqlServerDialect"/> with the specified <paramref name="pageSize"/>.
        /// </summary>
        /// <param name="pageSize">The maximum number of results to return when paging data.</param>
        public SqlServerDialect(Int32 pageSize)
        {
            Verify.GreaterThan(0, pageSize, "pageSize");

            this.pageSize = pageSize;
        }

        // Create Methods
        public DbCommand CreateCommand(String commandText) { return new SqlCommand(commandText); }
        public DbConnection CreateConnection(String connectionString) { return new SqlConnection(connectionString); }
        public DbParameter CreateTimestampParameter(DateTime timestamp) { return new SqlParameter("@timestamp", SqlDbType.DateTime2) { Value = timestamp }; }
        public DbParameter CreateCommitIdParameter(Guid commitId) { return new SqlParameter("@commitId", SqlDbType.UniqueIdentifier) { Value = commitId }; }
        public DbParameter CreateStreamIdParameter(Guid streamId) { return new SqlParameter("@streamId", SqlDbType.UniqueIdentifier) { Value = streamId }; }
        public DbParameter CreateVersionParameter(Int32 version) { return new SqlParameter("@version", SqlDbType.Int) { Value = version }; }
        public DbParameter CreateHeadersParameter(Byte[] headers) { return new SqlParameter("@headers", SqlDbType.VarBinary) { Value = headers }; }
        public DbParameter CreateEventsParameter(Byte[] events) { return new SqlParameter("@events", SqlDbType.VarBinary) { Value = events }; }
        public DbParameter CreateStateParameter(Byte[] state) { return new SqlParameter("@state", SqlDbType.VarBinary) { Value = state }; }
        public DbParameter CreateSkipParameter(Int32 skip) { return new SqlParameter("@skip", SqlDbType.Int) { Value = skip }; }
        public DbParameter CreateTakeParameter(Int32 take) { return new SqlParameter("@take", SqlDbType.Int) { Value = take }; }

        // Translate
        public Exception Translate(DbCommand command, DbException ex)
        {
            var sqlException = ex as SqlException;
            if (sqlException == null)
                return ex;

            if (sqlException.Number == UniqueIndexViolation)
            {
                var commitId = command.GetParameterValue("@commitId");

                return new DuplicateCommitException(Exceptions.DuplicateCommitException.FormatWith(commitId));
            }

            if (sqlException.Number == UniqueConstraintViolation)
            {
                var streamId = command.GetParameterValue("@streamId");
                var version =command.GetParameterValue("@version");

                return new ConcurrencyException(Exceptions.ConcurrencyException.FormatWith(streamId, version));
            }

            return ex;
        }
    }
}
