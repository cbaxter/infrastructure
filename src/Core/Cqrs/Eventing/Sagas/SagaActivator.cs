using System;
using System.Runtime.CompilerServices;

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// Creates an instance of the specified <see cref="Saga"/> type.
    /// </summary>
    public static class SagaActivator
    {
        /// <summary>
        /// Creates an instance of the specified <paramref name="sagaType"/>.
        /// </summary>
        /// <param name="sagaType">The saga type.</param>
        /// <param name="correlationId">The saga correlation identifier associated with this saga instance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Saga CreateInstance(Type sagaType, Guid correlationId)
        {
            var saga = (Saga)Activator.CreateInstance(sagaType);

            saga.Version = 0;
            saga.CorrelationId = correlationId;

            return saga;
        }

        /// <summary>
        /// Creates an instance of the specified saga type.
        /// </summary>
        /// <typeparam name="T">The saga type.</typeparam>
        /// <param name="correlationId">The saga correlation identifier associated with this saga instance.</param>
        public static T CreateInstance<T>(Guid correlationId)
            where T : Saga
        {
            return (T)CreateInstance(typeof(T), correlationId);
        }
    }
}
