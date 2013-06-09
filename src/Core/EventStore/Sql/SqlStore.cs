using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
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
    /// An SQL data sore.
    /// </summary>
    public abstract class SqlStore
    {
        private readonly ISerializeObjects serializer;
        private readonly String connectionString;
        private readonly ISqlDialect dialect;

        /// <summary>
        /// Initializes a new instance of <see cref="SqlStore"/>.
        /// </summary>
        /// <param name="connectionName">The name of the connection string associated with this <see cref="SqlStore"/>.</param>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> used to store binary data.</param>
        /// <param name="dialect">The database dialect associated with the <paramref name="connectionName"/>.</param>
        internal SqlStore(String connectionName, ISerializeObjects serializer, ISqlDialect dialect)
        {
            Verify.NotNull(dialect, "dialect");
            Verify.NotNull(serializer, "serializer");
            Verify.NotNullOrWhiteSpace(connectionName, "connectionName");

            this.dialect = dialect;
            this.serializer = serializer;
            this.connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
        }

        protected virtual DbConnection OpenConnection()
        {
            var connection = dialect.Provider.CreateConnection();

            Debug.Assert(connection != null);

            try
            {
                connection.ConnectionString = connectionString;
                connection.Open();
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }

            return connection;
        }

        /// <summary>
        /// Creates a new database connection.
        /// </summary>
        /// <param name="commandText">The SQL Statement associated with this <see cref="DbCommand"/>.</param>
        protected virtual DbCommand CreateCommand(String commandText)
        {
            var command = dialect.Provider.CreateCommand();

            Debug.Assert(command != null);

            command.CommandText = commandText;

            return command;
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        protected Int32 ExecuteNonQuery(DbCommand command)
        {
            return ExecuteCommand(command, command.ExecuteNonQuery);
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        protected Object ExecuteScalar(DbCommand command)
        {
            return ExecuteCommand(command, command.ExecuteScalar);
        }

        /// <summary>
        /// Queries the databse for a single database record returning default <typeparamref name="T"/> if not found.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="recordBuilder">The record builder that maps an <see cref="IDataRecord"/> to <typeparamref name="T"/>.</param>
        protected T QuerySingle<T>(DbCommand command, Func<IDataRecord, T> recordBuilder)
        {
            return ExecuteCommand(command, () => ExecuteReader(command, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, recordBuilder).SingleOrDefault());
        }

        /// <summary>
        /// Queries the databse for a zero or more database records.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="recordBuilder">The record builder that maps an <see cref="IDataRecord"/> to <typeparamref name="T"/>.</param>
        protected List<T> QueryMultiple<T>(DbCommand command, Func<IDataRecord, T> recordBuilder)
        {
            return ExecuteCommand(command, () => ExecuteReader(command, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, recordBuilder));
        }

        /// <summary>
        /// Builds a list of <typeparamref name="T"/> from the underlying <see cref="DbDataReader"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command">The command to execute.</param>
        /// <param name="commandBehavior">The <see cref="DbDataReader"/> command behavior.</param>
        /// <param name="recordBuilder">The record builder that maps an <see cref="IDataRecord"/> to <typeparamref name="T"/>.</param>
        private static List<T> ExecuteReader<T>(DbCommand command, CommandBehavior commandBehavior, Func<IDataRecord, T> recordBuilder)
        {
            var result = new List<T>();

            using (var dataReader = command.ExecuteReader(commandBehavior))
            {
                while (dataReader.Read())
                    result.Add(recordBuilder(dataReader));
            }

            return result;
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="executor">The executor used to execute the command.</param>
        private TResult ExecuteCommand<TResult>(DbCommand command, Func<TResult> executor)
        {
            TResult result;

            try
            {
                using (var connection = OpenConnection())
                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    command.Connection = connection;
                    command.Transaction = transaction;

                    result = executor();

                    transaction.Commit();
                }
            }
            catch (DbException ex)
            {
                throw dialect.Translate(command, ex);
            }

            return result;
        }

        /// <summary>
        /// Serializes an object graph to binary data.
        /// </summary>
        /// <param name="graph">The object graph to serialize.</param>
        protected Byte[] Serialize<T>(T graph)
        {
            return serializer.Serialize(graph);
        }

        /// <summary>
        /// Deserializes a binary field in to an object graph.
        /// </summary>
        /// <param name="buffer">The binary data to be deserialized in to an object graph.</param>
        protected T Deserialize<T>(Byte[] buffer)
        {
            return serializer.Deserialize<T>(buffer);
        }
    }
}
