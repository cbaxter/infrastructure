using System;
using System.Collections.Generic;

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

namespace Spark
{
    /// <summary>
    /// Extension methods of <see cref="Object"/>.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Returns true if a specified value is null; otherwise false.
        /// </summary>
        /// <param name="value">The string to test.</param>
        public static Boolean IsNull<T>(this T value)
            where T : class
        {
            return value == null;
        }

        /// <summary>
        /// Returns true if a specified value is not null; otherwise false.
        /// </summary>
        /// <param name="value">The string to test.</param>
        public static Boolean IsNotNull<T>(this T value)
            where T : class
        {
            return value != null;
        }
        
        /// <summary>
        /// Creates a new <see cref="Array"/> containing <paramref name="item"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="item"/>.</typeparam>
        /// <param name="item">The singleton element to be added to the <see cref="Array"/>.</param>
        public static T[] AsArray<T>(this T item)
        {
            return new[] { item };
        }

        /// <summary>
        /// Creates a new <see cref="List{T}"/> containing <paramref name="item"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="item"/>.</typeparam>
        /// <param name="item">The singleton element to be added to the <see cref="List{T}"/>.</param>
        public static List<T> AsList<T>(this T item)
        {
            return new List<T> { item };
        }
    }
}