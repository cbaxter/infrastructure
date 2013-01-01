using System;

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

namespace Spark.Infrastructure
{
    /// <summary>
    /// Extension methods of <see cref="String"/>.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Returns <paramref name="value"/> or <value>String.Empty</value> if <paramref name="value"/> is null.
        /// </summary>
        /// <param name="value">The value to return an empty string for if null.</param>
        public static String EmptyIfNull(this String value)
        {
            return value ?? String.Empty;
        }

        /// <summary>
        /// Replaces the format item in a specified string with the string representation of a corresponding object in a specified array.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="arg0">The object to format.</param>
        public static String FormatWith(this String format, Object arg0)
        {
            return String.Format(format, arg0);
        }

        /// <summary>
        /// Replaces the format item in a specified string with the string representation of a corresponding object in a specified array.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="arg0">The first object to format.</param>
        /// <param name="arg1">The second object to format.</param>
        public static String FormatWith(this String format, Object arg0, Object arg1)
        {
            return String.Format(format, arg0, arg1);
        }

        /// <summary>
        /// Replaces the format item in a specified string with the string representation of a corresponding object in a specified array.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="arg0">The first object to format.</param>
        /// <param name="arg1">The second object to format.</param>
        /// <param name="arg2">The third object to format.</param>
        public static String FormatWith(this String format, Object arg0, Object arg1, Object arg2)
        {
            return String.Format(format, arg0, arg1, arg2);
        }

        /// <summary>
        /// Replaces the format item in a specified string with the string representation of a corresponding object in a specified array.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static String FormatWith(this String format, params Object[] args)
        {
            return String.Format(format, args);
        }

        /// <summary>
        /// Returns true if a specified string is null; otherwise false.
        /// </summary>
        /// <param name="value">The string to test.</param>
        public static Boolean IsNull(this String value)
        {
            return value == null;
        }
        
        /// <summary>
        /// Returns true if a specified string is not null; otherwise false.
        /// </summary>
        /// <param name="value">The string to test.</param>
        public static Boolean IsNotNull(this String value)
        {
            return value != null;
        }
        
        /// <summary>
        /// Returns true if a specified string is empty; otherwise false.
        /// </summary>
        /// <param name="value">The string to test.</param>
        public static Boolean IsEmpty(this String value)
        {
            return value == String.Empty;
        }

        /// <summary>
        /// Returns true if a specified string is not empty; otherwise false.
        /// </summary>
        /// <param name="value">The string to test.</param>
        public static Boolean IsNotEmpty(this String value)
        {
            return value != String.Empty;
        }

        /// <summary>
        /// Returns true if a specified string is null or empty; otherwise false.
        /// </summary>
        /// <param name="value">The string to test.</param>
        public static Boolean IsNullOrEmpty(this String value)
        {
            return String.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Returns true if a specified string is not null or empty; otherwise false.
        /// </summary>
        /// <param name="value">The string to test.</param>
        public static Boolean IsNotNullOrEmpty(this String value)
        {
            return !String.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Returns true if a specified string is null, empty, or consists only of white-space characters; otherwise false.
        /// </summary>
        /// <param name="value">The string to test.</param>
        public static Boolean IsNullOrWhiteSpace(this String value)
        {
            return String.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Returns true if a specified string is not null, empty, or consists only of white-space characters; otherwise false.
        /// </summary>
        /// <param name="value">The string to test.</param>
        public static Boolean IsNotNullOrWhiteSpace(this String value)
        {
            return !String.IsNullOrWhiteSpace(value);
        }
    }
}
