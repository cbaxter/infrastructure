﻿using System;
using System.Collections.Generic;
using System.Threading;
using Spark.Resources;

/* Copyright (c) 2014 Spark Software Ltd.
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
            if (lockReference != null) throw new InvalidOperationException(Exceptions.AggregateLockAlreadyHeld.FormatWith(AggregateType, Aggregateid));

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
            if (lockReference == null) throw new InvalidOperationException(Exceptions.AggregateLockNotHeld.FormatWith(AggregateType, Aggregateid));

            Monitor.Exit(lockReference);

            lock (GlobalLock)
            {
                lockReference.Remove(this);

                if (lockReference.Count == 0)
                    AggregateLocks.Remove(id);
            }

            lockReference = null;
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="AggregateLock"/> class.
        /// </summary>
        public void Dispose()
        {
            if (lockReference == null)
                return;

            Release();
        }
    }
}