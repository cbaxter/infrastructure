using System;
using System.Configuration;
using System.Data.SqlClient;
using Xunit;

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

namespace Test.Spark.EventStore.Dialects
{
    /// <summary>
    /// Constant for configured SQL-Server connection name.
    /// </summary>
    public static class SqlServerConnection
    {
        public const String Name = "Test.SqlServer";
        private static readonly String ConnectionString = ConfigurationManager.ConnectionStrings[Name].ConnectionString;

        /// <summary>
        /// Creates a new SQL-Server connection.
        /// </summary>
        /// <returns></returns>
        public static SqlConnection Create()
        {
            return new SqlConnection(ConnectionString);
        }
    }

    /// <summary>
    /// SQL-Server Integration Test Fact Attribute.
    /// </summary>
    public sealed class SqlServerFactAttribute : FactAttribute
    {
        private static readonly String ConnectionString = ConfigurationManager.ConnectionStrings[SqlServerConnection.Name].ConnectionString;
        private static readonly String SkipReason;

        /// <summary>
        /// Determine if the configured SQL-Server instance is available.
        /// </summary>
        static SqlServerFactAttribute()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                    connection.Open();

                SkipReason = null;
            }
            catch (Exception ex)
            {
                SkipReason = ex.Message;
            }
        }

        /// <summary>
        /// Ensure Skip reason set if SQL-Server instance is not available.
        /// </summary>
        public SqlServerFactAttribute()
        {
            Skip = SkipReason;
        }
    }
}
