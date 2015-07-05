using System;
using System.Collections.Generic;
using System.Threading;
using Spark.Resources;

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
    /// A <see cref="Aggregate"/> synchronization lock object.
    /// </summary>
    internal sealed class AggregateLock : IDisposable
    {
        private static readonly IDictionary<Guid, HashSet<AggregateLock>> AggregateLocks = new Dictionary<Guid, HashSet<AggregateLock>>();
        private static readonly Object GlobalLock = new Object();
        private HashSet<AggregateLock> lockReference;
        private readonly Type aggregateType;
        private readonly Guid id;

        /// <summary>
        /// The underlying aggregate <see cref="Type"/> associated with this aggregate lock instance.
        /// </summary>
        public Type AggregateType { get { return aggregateType; } }

        /// <summary>
        /// The underlying aggregate correlation ID associated with this aggregate lock instance.
        /// </summary>
        public Guid Aggregateid { get { return id; } }

        /// <summary>
        /// Indicates if the underlying aggregate lock has been aquired.
        /// </summary>
        public Boolean Aquired { get { return lockReference != null; } }

        /// <summary>
        /// Initializes a new instance of <see cref="AggregateLock"/>.
        /// </summary>
        /// <param name="aggregateType">The aggregate type associated with this aggregate lock instance.</param>
        /// <param name="id">The aggregate identifier associated with this aggregate lock instance.</param>
        public AggregateLock(Type aggregateType, Guid id)
        {
            this.aggregateType = aggregateType;
            this.id = id;
        }

        /// <summary>
        /// Aquires the lock on the specified aggregate instance identified by <see cref="Aggregateid"/>.
        /// </summary>
        public void Aquire()
        {
            if (Aquired) throw new InvalidOperationException(Exceptions.AggregateLockAlreadyHeld.FormatWith(AggregateType, Aggregateid));

            lock (GlobalLock)
            {
                if (!AggregateLocks.TryGetValue(id, out lockReference))
                    AggregateLocks.Add(id, lockReference = new HashSet<AggregateLock>());

                lockReference.Add(this);
            }

            Monitor.Enter(lockReference);
        }

        /// <summary>
        /// Releases the lock on the specified aggregate instance identified by <see cref="Aggregateid"/>.
        /// </summary>
        public void Release()
        {
            if (!Aquired) throw new InvalidOperationException(Exceptions.AggregateLockNotHeld.FormatWith(AggregateType, Aggregateid));

            Monitor.Exit(lockReference);

            lock (GlobalLock)
            {
                lockReference.Remove(this);

                if (lockReference.Count == 0)
                    AggregateLocks.Remove(id);

                lockReference = null;
            }
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="AggregateLock"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!Aquired)
                return;

            Release();
        }
    }
}