using System;

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
    /// Represents a scheduled saga timeout.
    /// </summary>
    public struct SagaTimeout : IEquatable<SagaTimeout>
    {
        private readonly Guid sagaId;
        private readonly Type sagaType;
        private readonly DateTime timeout;

        /// <summary>
        /// The saga <see cref="Type"/> associated with this saga timeout instance.
        /// </summary>
        public Type SagaType { get { return sagaType; } }

        /// <summary>
        /// The saga correlation ID associated with this saga timeout instance.
        /// </summary>
        public Guid SagaId { get { return sagaId; } }

        /// <summary>
        /// The date/time associated with this saga timeout instance.
        /// </summary>
        public DateTime Timeout { get { return timeout; } }

        /// <summary>
        /// Initializes a new instance of <see cref="SagaTimeout"/>.
        /// </summary>
        /// <param name="sagaId">The saga correlation ID associated with this saga timeout instance.</param>
        /// <param name="sagaType">The saga <see cref="Type"/> associated with this saga timeout instance.</param>
        /// <param name="timeout">The date/time associated with this saga timeout instance.</param>
        public SagaTimeout(Type sagaType, Guid sagaId, DateTime timeout)
        {
            Verify.NotNull(sagaType, nameof(sagaType));

            this.sagaId = sagaId;
            this.sagaType = sagaType;
            this.timeout = timeout;
        }      
        
        /// <summary>
        /// Indicates whether this instance and a specified <see cref="Object"/> are equal.
        /// </summary>
        /// <param name="other">Another object to compare.</param>
        public override Boolean Equals(Object other)
        {
            return other is SagaTimeout && Equals((SagaTimeout)other);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="SagaTimeout"/> are equal.
        /// </summary>
        /// <param name="other">Another <see cref="SagaTimeout"/> to compare.</param>
        public Boolean Equals(SagaTimeout other)
        {
            return sagaType == other.sagaType && sagaId == other.sagaId && timeout == other.timeout;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {
                var hash = 43;

                hash = (hash * 397) + SagaType.GetHashCode();
                hash = (hash * 397) + SagaId.GetHashCode();
                hash = (hash * 397) + Timeout.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns the <see cref="SagaTimeout"/> description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} - {1} @ {2}", sagaType, sagaId, timeout);
        }

        /// <summary>
        /// Implicitly converts a <see cref="SagaTimeout"/> to the corresponding <see cref="SagaReference"/>.
        /// </summary>
        /// <param name="sagaTimeout">The saga timeout for which a saga reference is to be constructed.</param>
        public static implicit operator SagaReference(SagaTimeout sagaTimeout)
        {
            return new SagaReference(sagaTimeout.SagaType, sagaTimeout.SagaId);
        }
    }
}
