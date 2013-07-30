using System;
using System.Collections.Generic;
using System.Threading;
using Spark.Resources;

/* Copyright (c) 2013 Spark Software Ltd.
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

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// A <see cref="Saga"/> synchronization lock object.
    /// </summary>
    internal sealed class SagaLock : IDisposable
    {
        private static readonly IDictionary<SagaReference, HashSet<SagaLock>> SagaLocks = new Dictionary<SagaReference, HashSet<SagaLock>>();
        private static readonly Object GlobalLock = new Object();
        private readonly SagaReference sagaReference;
        private HashSet<SagaLock> lockReference;

        /// <summary>
        /// The underlying saga <see cref="Type"/> associated with this saga lock instance.
        /// </summary>
        public Type SagaType { get { return sagaReference.SagaType; } }

        /// <summary>
        /// The underlying saga correlation ID associated with this saga lock instance.
        /// </summary>
        public Guid SagaId { get { return sagaReference.SagaId; } }

        /// <summary>
        /// Initializes a new instance of <see cref="SagaLock"/>.
        /// </summary>
        /// <param name="sagaType">The saga type associated with this saga lock instance.</param>
        /// <param name="sagaId">The saga correlation ID associated with this saga lock instance.</param>
        public SagaLock(Type sagaType, Guid sagaId)
        {
            sagaReference = new SagaReference(sagaType, sagaId);
        }

        /// <summary>
        /// Aquires the lock on the specified saga instance identified by <see cref="SagaType"/> and <see cref="SagaId"/>.
        /// </summary>
        public void Aquire()
        {
            if(lockReference != null)
                throw new InvalidOperationException(Exceptions.SagaLockAlreadyHeld.FormatWith(SagaType, SagaId));

            lock (GlobalLock)
            {
                if (!SagaLocks.TryGetValue(sagaReference, out lockReference))
                    SagaLocks.Add(sagaReference, lockReference = new HashSet<SagaLock>());

                lockReference.Add(this);
            }

            Monitor.Enter(lockReference);
        }

        /// <summary>
        /// Releases the lock on the specified saga instance identified by <see cref="SagaType"/> and <see cref="SagaId"/>.
        /// </summary>
        public void Release()
        {
            if (lockReference == null)
                throw new InvalidOperationException(Exceptions.SagaLockNotHeld.FormatWith(SagaType, SagaId));

            Monitor.Exit(lockReference);

            lock (GlobalLock)
            {
                lockReference.Remove(this);

                if (lockReference.Count == 0)
                    SagaLocks.Remove(sagaReference);
            }

            lockReference = null;
        }
        
        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="SagaContext"/> class.
        /// </summary>
        public void Dispose()
        {
            if (lockReference == null)
                return;

            Release();
        }
    }
}
