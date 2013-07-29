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
    /// The <see cref="Event"/> raised when a saga timeout has elapsed.
    /// </summary>
    public sealed class Timeout : Event
    {
        /// <summary>
        /// The saga correlation identifier associated with this saga timeout event.
        /// </summary>
        public Guid SagaId { get { return AggregateId; } }

        /// <summary>
        /// The saga type associated with this saga timeout event.
        /// </summary>
        public Type SagaType { get; private set; }

        /// <summary>
        /// The date/time of when the scheduled timeout occurred.
        /// </summary>
        /// <remarks>
        /// May access the timestamp in the <see cref="EventContext.Headers"/> collection to determine when the timeout event was published.
        /// </remarks>
        public DateTime Scheduled { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Timeout"/>.
        /// </summary>
        /// <param name="sagaType">The saga type associated with this timeout instance.</param>
        /// <param name="scheduled">The date/time of when the scheduled timeout occurred.</param>
        public Timeout(Type sagaType, DateTime scheduled)
        {
            Verify.NotNull(sagaType, "sagaType");

            this.SagaType = sagaType;
            this.Scheduled = scheduled;
        }
    }
}
