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
