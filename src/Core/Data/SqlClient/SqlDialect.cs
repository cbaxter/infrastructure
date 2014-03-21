using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
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

namespace Spark.Data.SqlClient
{
    /// <summary>
    /// The base SQL RDBMS dialect.
    /// </summary>
    public abstract class SqlDialect : IDbDialect
    {
        private readonly String connectionString;

        /// <summary>
        /// The <see cref="DbParameter"/> size to use when using (MAX) for VARBINARY or VARCHAR fields.
        /// </summary>
        protected const Int32 Max = -1;

        /// <summary>
        /// The Sql-Server databse provider factory.
        /// </summary>
        public DbProviderFactory Provider { get { return SqlClientFactory.Instance; } }

        /// <summary>
        /// The Sql-Server database connection string.
        /// </summary>
        public String ConnectionString { get { return connectionString; } }

        /// <summary>
        /// Initializes a new instance of <see cref="SqlDialect"/>.
        /// </summary>
        /// <param name="connectionName">The connection name used by this sql dialect instance.</param>
        internal SqlDialect(String connectionName)
        {
            Verify.NotNullOrWhiteSpace(connectionName, "connectionName");

            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionName];
            if(connectionStringSettings == null)
                throw new KeyNotFoundException(Exceptions.ConnectionNotFound);

            connectionString = connectionStringSettings.ConnectionString;
        }

        /// <summary>
        /// Translate the specified <see cref="DbException"/> if required.
        /// </summary>
        /// <param name="command">The <see cref="IDbCommand"/> that generated the exception.</param>
        /// <param name="ex">The exception to translate.</param>
        public virtual Exception Translate(IDbCommand command, DbException ex)
        {
            return ex;
        }
    }
}
