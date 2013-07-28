using System;

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
    /// Represents a scheduled saga timeout.
    /// </summary>
    public struct SagaTimeout
    {
        private readonly Guid sagaId;
        private readonly Type sagaType;
        private readonly Int32 version;
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
        /// The saga version associated with this saga timeout instance.
        /// </summary>
        public Int32 Version { get { return version; } }

        /// <summary>
        /// The date/time associated with this saga timeout instance.
        /// </summary>
        public DateTime Timeout { get { return timeout; } }

        /// <summary>
        /// Initializes a new instance of <see cref="SagaTimeout"/>.
        /// </summary>
        /// <param name="sagaId">The saga correlation ID associated with this saga timeout instance.</param>
        /// <param name="sagaType">The saga <see cref="Type"/> associated with this saga timeout instance.</param>
        /// <param name="version">The saga version associated with this saga timeout instance.</param>
        /// <param name="timeout">The date/time associated with this saga timeout instance.</param>
        public SagaTimeout(Guid sagaId, Type sagaType, Int32 version, DateTime timeout)
        {
            Verify.NotNull(sagaType, "sagaType");

            this.sagaId = sagaId;
            this.sagaType = sagaType;
            this.version = version;
            this.timeout = timeout;
        }
    }
}
