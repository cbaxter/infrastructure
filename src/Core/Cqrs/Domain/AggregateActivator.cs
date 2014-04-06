using System;
using System.Runtime.CompilerServices;

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
