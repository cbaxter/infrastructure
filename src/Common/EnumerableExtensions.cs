using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Spark.Infrastructure
{
    /// <summary>
    /// Extension methods of <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Cast <paramref name="source"/> as an <see cref="IList{T}"/> if possible; otherwise create a new <see cref="List{T}"/> wrapper.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to cast as an <see cref="IList{T}"/> or create a <see cref="List{T}"/> from.</param>
        public static IList<T> AsList<T>(this IEnumerable<T> source)
        {
            return source == null ? null : source as IList<T> ?? new List<T>(source);
        }

        /// <summary>
        /// Cast <paramref name="source"/> as an <see cref="IReadOnlyList{T}"/> if possible; otherwise create a new <see cref="ReadOnlyCollection{T}"/> wrapper.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to cast as an <see cref="IReadOnlyList{T}"/> or create a <see cref="ReadOnlyCollection{T}"/> from.</param>
        public static IReadOnlyList<T> AsReadOnly<T>(this IEnumerable<T> source)
        {
            return source == null ? null : source as IReadOnlyList<T> ?? new ReadOnlyCollection<T>(source.AsList());
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
