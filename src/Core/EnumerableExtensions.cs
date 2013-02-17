using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Spark.Infrastructure
{
    /// <summary>
    /// Extension methods of <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Cast <paramref name="source"/> as an array of <typeparamref name="T"/> if possible; otherwise create a new array of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of items in <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to cast as an <see cref="IList{T}"/> or create a <see cref="List{T}"/> from.</param>
        public static T[] AsArray<T>(this IEnumerable<T> source)
        {
            return source as T[] ?? source.EmptyIfNull().ToArray();
        }

        /// <summary>
        /// Cast <paramref name="source"/> as an <see cref="IList{T}"/> if possible; otherwise create a new <see cref="List{T}"/> wrapper.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to cast as an <see cref="IList{T}"/> or create a <see cref="List{T}"/> from.</param>
        public static IList<T> AsList<T>(this IEnumerable<T> source)
        {
            return source as IList<T> ?? new List<T>(source.EmptyIfNull());
        }

        /// <summary>
        /// Cast <paramref name="source"/> as an <see cref="IReadOnlyList{T}"/> if possible; otherwise create a new <see cref="ReadOnlyCollection{T}"/> wrapper.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to cast as an <see cref="IReadOnlyList{T}"/> or create a <see cref="ReadOnlyCollection{T}"/> from.</param>
        public static IReadOnlyList<T> AsReadOnly<T>(this IEnumerable<T> source)
        {
            return source as IReadOnlyList<T> ?? new ReadOnlyCollection<T>(source.AsList());
        }

        /// <summary>
        /// Appends the <paramref name="item"/> to the end of the existing <paramref name="source"/> set.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> set on which to append <paramref name="item"/>.</param>
        /// <param name="item">The item to append to <paramref name="source"/>.</param>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T item)
        {
            foreach (var value in source ?? Enumerable.Empty<T>())
                yield return value;

            yield return item;
        }
        
        /// <summary>
        /// Returns distinct elements from a sequence by using the the default equality comparer to compare <paramref name="keySelector"/> values.
        /// </summary>
        /// <param name="source">The sequence of elements to make distinct.</param>
        /// <param name="keySelector">The key selector on which to base distinct values.</param>
        public static IEnumerable<TItem> Distinct<TItem, TKey>(this IEnumerable<TItem> source, Func<TItem, TKey> keySelector)
        {
            var unique = new HashSet<TKey>();
            return (source ?? Enumerable.Empty<TItem>()).Where(item => unique.Add(keySelector(item)));
        }

        /// <summary>
        /// Returns the elements of the specified sequence or an empty sequence if <paramref name="source"/> is null.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">The sequence to return an empty enumerable for if null.</param>
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }
    }
}
