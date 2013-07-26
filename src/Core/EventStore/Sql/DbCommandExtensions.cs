using System;
using System.Data.Common;
using System.Linq;

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

namespace Spark.EventStore.Sql
{
    /// <summary>
    /// Extension methods of <see cref="DbCommand"/>.
    /// </summary>
    internal static class DbCommandExtensions
    {
        /// <summary>
        /// Creates a new <see cref="DbCommand"/> that is a copy of the current instance.
        /// </summary>
        /// <param name="command">The <see cref="DbCommand"/> to clone.</param>
        public static DbCommand Clone(this DbCommand command)
        {
            // NOTE: All known implementations of DbCommand also implement ICloneable; should this change method will need to be more robust.
            return (DbCommand)((ICloneable)command).Clone();
        }

        /// <summary>
        /// Gets the value of the specified command parameter or null if not found.
        /// </summary>
        /// <param name="command">The command on which to locate a named parameter.</param>
        /// <param name="parameterName">The name of the parameter to locate.</param>
        public static Object GetParameterValue(this DbCommand command, String parameterName)
        {
            return command.Parameters.Cast<DbParameter>().Where(parameter => parameter.ParameterName == parameterName).Select(parameter => parameter.Value).SingleOrDefault();
        }
    }
}
