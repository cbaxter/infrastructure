using System;
using System.Runtime.CompilerServices;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Spark.Cqrs.Domain
{
    /// <summary>
    /// Creates an instance of the specified <see cref="Aggregate"/> type.
    /// </summary>
    public static class AggregateActivator
    {
        /// <summary>
        /// Creates an instance of the specified <paramref name="aggregateType"/>.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        /// <param name="id">The identifier associated with this aggregate instance.</param>
        /// <param name="version">The aggregate revision used to detect concurrency conflicts.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Aggregate CreateInstance(Type aggregateType, Guid id, Int32 version)
        {
            var saga = (Aggregate)Activator.CreateInstance(aggregateType);

            saga.Id = id;
            saga.Version = version;

            return saga;
        }

        /// <summary>
        /// Creates an instance of the specified saga type.
        /// </summary>
        /// <typeparam name="T">The saga type.</typeparam>
        /// <param name="id">The identifier associated with this aggregate instance.</param>
        /// <param name="version">The saga version used to detect concurrency conflicts.</param>
        public static T CreateInstance<T>(Guid id, Int32 version)
            where T : Aggregate
        {
            return (T)CreateInstance(typeof(T), id, version);
        }
    }
}
