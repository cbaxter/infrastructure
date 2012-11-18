using System;
using System.Collections.Generic;

namespace Spark.Infrastructure
{
    /// <summary>
    /// Extension methods of <see cref="Object"/>.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Creates a new <see cref="Array"/> containing <paramref name="item"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="item"/>.</typeparam>
        /// <param name="item">The singleton element to be added to the <see cref="Array"/>.</param>
        public static T[] ToArray<T>(this T item)
        {
            return new[] { item };
        }

        /// <summary>
        /// Creates a new <see cref="List{T}"/> containing <paramref name="item"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="item"/>.</typeparam>
        /// <param name="item">The singleton element to be added to the <see cref="List{T}"/>.</param>
        public static List<T> ToList<T>(this T item)
        {
            return new List<T> { item };
        }
    }
}