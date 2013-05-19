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

namespace Spark.Infrastructure.EventStore.Sql.Dialects
{
    internal sealed class SqlServerDialect : IEventStoreDialect, ISnapshotStoreDialect
    {
        public const Int32 UniqueIndexViolation = 2601;
        public const Int32 UniqueConstraintViolation = 2627;

        // IEventStoreDialect
        public String GetRange { get { return SqlServerDialectStatements.GetRange; } }
        public String GetStream { get { return SqlServerDialectStatements.GetStream; } }
        public String GetStreams { get { return SqlServerDialectStatements.GetStreams; } }
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
        public DbCommand CreateCommand(String commandText) { return new SqlCommand(commandText); }
        public DbConnection CreateConnection(String connectionString) { return new SqlConnection(connectionString); }
        public DbParameter CreateIdParameter(Guid commitId) { return new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = commitId }; }
        public DbParameter CreateTimestampParameter(DateTime timestamp) { return new SqlParameter("@timestamp", SqlDbType.DateTime2) { Value = timestamp }; }
        public DbParameter CreateStreamIdParameter(Guid streamId) { return new SqlParameter("@streamId", SqlDbType.UniqueIdentifier) { Value = streamId }; }
        public DbParameter CreateVersionParameter(Int32 version) { return new SqlParameter("@version", SqlDbType.Int) { Value = version }; }
        public DbParameter CreateHeadersParameter(Byte[] headers) { return new SqlParameter("@headers", SqlDbType.VarBinary) { Value = headers }; }
        public DbParameter CreateEventsParameter(Byte[] events) { return new SqlParameter("@events", SqlDbType.VarBinary) { Value = events }; }
        public DbParameter CreateStateParameter(Byte[] state) { return new SqlParameter("@state", SqlDbType.VarBinary) { Value = state }; }
        public DbParameter CreateSkipParameter(Int64 skip) { return new SqlParameter("@skip", SqlDbType.BigInt) { Value = skip }; }
        public DbParameter CreateTakeParameter(Int64 take) { return new SqlParameter("@take", SqlDbType.BigInt) { Value = take }; }

        // Translate
        public Exception Translate(DbCommand command, DbException ex)
        {
            var sqlException = ex as SqlException;
            if (sqlException == null)
                return ex;

            if (sqlException.Number == UniqueConstraintViolation)
            {
                var commitId = command.GetParameterValue("@id");

                return new DuplicateCommitException(Exceptions.DuplicateCommitException.FormatWith(commitId));
            }

            if (sqlException.Number == UniqueIndexViolation)
            {
                var streamId = command.GetParameterValue("@streamId");
                var version = command.GetParameterValue("@version");

                return new ConcurrencyException(Exceptions.ConcurrencyException.FormatWith(streamId, version));
            }

            return ex;
        }
    }
}
