using System;
using System.Collections.Generic;
using System.Configuration;
using Spark.Infrastructure.EventStore.Sql.Dialects;
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

namespace Spark.Infrastructure.EventStore.Sql
{
    /// <summary>
    /// Gets the appropriate RDBMS dialect based on the specified connection provider name.
    /// </summary>
    /// <remarks>
    /// Although segregated interfaces exist for <see cref="IEventStoreDialect"/> and <see cref="ISnapshotStoreDialect"/>; typically will be
    /// implemented by the same concrete dialect class to keep everything together. If the need arises the <see cref="GetDialect"/> method
    /// can be split to allow seperate support for <see cref="IEventStoreDialect"/> and <see cref="ISnapshotStoreDialect"/>.
    /// </remarks>
    internal static class DialectProvider
    {
        /// <summary>
        /// Gets the <see cref="ISnapshotStoreDialect"/> associated with the specified <paramref name="connectionName"/>.
        /// </summary>
        /// <param name="connectionName">The name of the connection string from which to construct the <see cref="ISnapshotStoreDialect"/>.</param>
        public static ISnapshotStoreDialect GetSnapshotStoreDialect(String connectionName)
        {
            return (ISnapshotStoreDialect)GetDialect(connectionName);
        }

        /// <summary>
        /// Gets the <see cref="IEventStoreDialect"/> associated with the specified <paramref name="connectionName"/>.
        /// </summary>
        /// <param name="connectionName">The name of the connection string from which to construct the <see cref="IEventStoreDialect"/>.</param>
        public static IEventStoreDialect GetEventStoreDialect(String connectionName)
        {
            return (IEventStoreDialect)GetDialect(connectionName);
        }

        /// <summary>
        /// Maps the provider name associated with the <paramref name="connectionName"/> to an RDBMS dialect.
        /// </summary>
        /// <param name="connectionName">The name of the connection string from which to construct the <see cref="ISqlDialect"/>.</param>
        private static ISqlDialect GetDialect(String connectionName)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[connectionName];
            if (connectionString == null)
                throw new KeyNotFoundException(Exceptions.ConnectionNotFound.FormatWith(connectionName));

            var providerName = connectionString.ProviderName;
            if (providerName.IsNullOrWhiteSpace())
                throw new InvalidOperationException(Exceptions.ConnectionProviderNotSpecified.FormatWith(connectionName));

            if (providerName.Equals("System.Data.SqlClient", StringComparison.InvariantCultureIgnoreCase))
                return new SqlServerDialect();

            throw new NotSupportedException(Exceptions.UnknownDialect.FormatWith(providerName));
        }
    }
}
