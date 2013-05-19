using System;
using System.Data.Common;

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
    /// Base SQL RDBMS dialect contract.
    /// </summary>
    internal interface ISqlDialect
    {
        /// <summary>
        /// Create a new <see cref="DbConnection"/>.
        /// </summary>
        /// <param name="connectionString">The connection string for this connection.</param>
        DbConnection CreateConnection(String connectionString);

        /// <summary>
        /// Create a new <see cref="DbCommand"/>.
        /// </summary>
        /// <param name="commandText">The command text for this command.</param>
        DbCommand CreateCommand(String commandText);

        /// <summary>
        /// Translate the specified <see cref="DbException"/> if required.
        /// </summary>
        /// <param name="command">The <see cref="DbCommand"/> that generated the exception.</param>
        /// <param name="ex">The exception to translate.</param>
        Exception Translate(DbCommand command, DbException ex);
    }
}
