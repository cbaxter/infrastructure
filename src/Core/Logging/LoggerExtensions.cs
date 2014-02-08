using System;
/* Copyright (c) 2014 Spark Software Ltd.
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
using JetBrains.Annotations;

namespace Spark.Logging
{
    /// <summary>
    /// Extension methods of <see cref="ILog"/>.
    /// </summary>
    public static class LogExtensions
    {
        /// <summary>
        /// Start a new logical operation.
        /// </summary>
        /// <param name="log">The logger instance to extend.</param>
        public static IDisposable PushContext(this ILog log)
        {
            Verify.NotNull(log, "log");

            return log.PushContext(Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Start a new named logical operation.
        /// </summary>
        /// <param name="log">The logger instance to extend.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="arg0">The object to format.</param>
        [StringFormatMethod("format")]
        public static IDisposable PushContext(this ILog log, String format, Object arg0)
        {
            Verify.NotNull(log, "log");

            return log.PushContext(String.Format(format, arg0));
        }

        /// <summary>
        /// Start a new named logical operation.
        /// </summary>
        /// <param name="log">The logger instance to extend.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="arg0">The first object to format.</param>
        /// <param name="arg1">The second object to format.</param>
        [StringFormatMethod("format")]
        public static IDisposable PushContext(this ILog log, String format, Object arg0, Object arg1)
        {
            Verify.NotNull(log, "log");

            return log.PushContext(String.Format(format, arg0, arg1));
        }

        /// <summary>
        /// Start a new named logical operation.
        /// </summary>
        /// <param name="log">The logger instance to extend.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="arg0">The first object to format.</param>
        /// <param name="arg1">The second object to format.</param>
        /// <param name="arg2">The third object to format.</param>
        [StringFormatMethod("format")]
        public static IDisposable PushContext(this ILog log, String format, Object arg0, Object arg1, Object arg2)
        {
            Verify.NotNull(log, "log");

            return log.PushContext(String.Format(format, arg0, arg1, arg2));
        }

        /// <summary>
        /// Start a new named logical operation.
        /// </summary>
        /// <param name="log">The logger instance to extend.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more items to format.</param>
        [StringFormatMethod("format")]
        public static IDisposable PushContext(this ILog log, String format, params Object[] args)
        {
            Verify.NotNull(log, "log");

            return log.PushContext(String.Format(format, args));
        }
    }
}
