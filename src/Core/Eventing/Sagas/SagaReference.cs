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
    /// Uniquely identifies a saga instance.
    /// </summary>
    public struct SagaReference : IEquatable<SagaReference>
    {
        private readonly Type sagaType;
        private readonly Guid sagaId;

        /// <summary>
        /// The saga <see cref="Type"/> associated with this saga reference instance.
        /// </summary>
        public Type SagaType { get { return sagaType; } }

        /// <summary>
        /// The saga <see cref="Type"/> associated with this saga reference instance.
        /// </summary>
        public Guid SagaId { get { return sagaId; } }

        /// <summary>
        /// Initializes a new instance of <see cref="SagaReference"/> with the specified <paramref name="sagaType"/> and <paramref name="sagaId"/>.
        /// </summary>
        /// <param name="sagaType">The saga <see cref="Type"/> associated with this saga reference instance.</param>
        /// <param name="sagaId">The saga <see cref="Type"/> associated with this saga reference instance.</param>
        public SagaReference(Type sagaType, Guid sagaId)
        {
            Verify.NotNull(sagaType, "sagaType");
            Verify.NotEqual(Guid.Empty, sagaId, "sagaId");

            this.sagaType = sagaType;
            this.sagaId = sagaId;
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="Object"/> are equal.
        /// </summary>
        /// <param name="other">Another object to compare.</param>
        public override Boolean Equals(Object other)
        {
            return other is SagaReference && Equals((SagaReference)other);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="SagaReference"/> are equal.
        /// </summary>
        /// <param name="other">Another <see cref="SagaReference"/> to compare.</param>
        public Boolean Equals(SagaReference other)
        {
            return sagaType == other.sagaType && sagaId == other.sagaId;
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

                return hash;
            }
        }

        /// <summary>
        /// Returns the page description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} - {1}", SagaType, SagaId);
        }
    }
}
