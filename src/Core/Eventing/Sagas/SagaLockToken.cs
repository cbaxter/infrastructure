using System;

/* Copyright (c) 2012 Spark Software Ltd.
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

namespace Spark.Infrastructure.Eventing.Sagas
{
    /// <summary>
    /// Represents an aquired <see cref="Saga"/> instnace lock reference.
    /// </summary>
    internal sealed class SagaLockToken : IDisposable
    {
        private readonly SagaReference sagaReference;
        private readonly SagaLock sagaLock;
        private Boolean disposed;

        /// <summary>
        /// The saga reference associated with this saga lock token instance.
        /// </summary>
        internal SagaReference Reference { get { return sagaReference; } }

        /// <summary>
        /// The saga lock reference associated with this saga lock token instance.
        /// </summary>
        internal SagaLock SagaLock { get { return sagaLock; } }

        /// <summary>
        /// Intializes a new instance of <see cref="SagaLockToken"/> for the specified <paramref name="sagaReference"/> and <paramref name="sagaLock"/>.
        /// </summary>
        /// <param name="sagaReference">The saga reference associated with this saga lock token instance.</param>
        /// <param name="sagaLock">The saga lock reference associated with this saga lock token instance.</param>
        internal SagaLockToken(SagaReference sagaReference, SagaLock sagaLock)
        {
            this.sagaReference = sagaReference;
            this.sagaLock = sagaLock;
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="SagaLockToken"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Saga.ReleaseLock(this);
        }
    }
}
