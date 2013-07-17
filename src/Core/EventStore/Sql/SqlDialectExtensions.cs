using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;

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
    /// Extension methods of <see cref="ISqlDialect"/>.
    /// </summary>
    internal static class SqlDialectExtensions
    {
        /// <summary>
        /// Opens a new database connection.
        /// </summary>
        /// <param name="dialect">The <see cref="ISqlDialect"/>.</param>
        public static DbConnection OpenConnection(this ISqlDialect dialect)
        {
            var connection = dialect.Provider.CreateConnection();

            Debug.Assert(connection != null);

            try
            {
                connection.ConnectionString = dialect.ConnectionString;
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
        /// Creates a new database command.
        /// </summary>
        /// <param name="dialect">The <see cref="ISqlDialect"/>.</param>
        /// <param name="commandText">The SQL statement associated with this <see cref="DbCommand"/>.</param>
        public static DbCommand CreateCommand(this ISqlDialect dialect, String commandText)
        {
            var command = dialect.Provider.CreateCommand();

            Debug.Assert(command != null);

            command.CommandText = commandText;

            return command;
        }

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <param name="dialect">The <see cref="ISqlDialect"/>.</param>
        /// <param name="command">The command to execute.</param>
        public static Int32 ExecuteNonQuery(this ISqlDialect dialect, DbCommand command)
        {
            return dialect.ExecuteCommand(command, command.ExecuteNonQuery);
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <param name="dialect">The <see cref="ISqlDialect"/>.</param>
        /// <param name="command">The command to execute.</param>
        public static Object ExecuteScalar(this ISqlDialect dialect, DbCommand command)
        {
            return dialect.ExecuteCommand(command, command.ExecuteScalar);
        }

        /// <summary>
        /// Queries the databse for a single database record returning default <typeparamref name="T"/> if not found.
        /// </summary>
        /// <param name="dialect">The <see cref="ISqlDialect"/>.</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="recordBuilder">The record builder that maps an <see cref="IDataRecord"/> to <typeparamref name="T"/>.</param>
        public static T QuerySingle<T>(this ISqlDialect dialect, DbCommand command, Func<IDataRecord, T> recordBuilder)
        {
            return dialect.ExecuteCommand(command, () => ExecuteReader(command, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, recordBuilder).SingleOrDefault());
        }

        /// <summary>
        /// Queries the databse for a zero or more database records.
        /// </summary>
        /// <param name="dialect">The <see cref="ISqlDialect"/>.</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="recordBuilder">The record builder that maps an <see cref="IDataRecord"/> to <typeparamref name="T"/>.</param>
        public static List<T> QueryMultiple<T>(this ISqlDialect dialect, DbCommand command, Func<IDataRecord, T> recordBuilder)
        {
            return dialect.ExecuteCommand(command, () => ExecuteReader(command, CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, recordBuilder));
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
        /// <param name="dialect">The <see cref="ISqlDialect"/>.</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="executor">The executor used to execute the command.</param>
        private static TResult ExecuteCommand<TResult>(this ISqlDialect dialect, DbCommand command, Func<TResult> executor)
        {
            TResult result;

            try
            {
                using (var connection = dialect.OpenConnection())
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
    }
}
