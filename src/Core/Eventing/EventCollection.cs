using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

namespace Spark.Infrastructure.Eventing
{
    /// <summary>
    /// A read-only collection of events.
    /// </summary>
    [Serializable]
    public sealed class EventCollection : ReadOnlyCollection<Event>
    {
        /// <summary>
        /// Represents an empty <see cref="EventCollection"/>. This field is read-only.
        /// </summary>
        public static readonly EventCollection Empty = new EventCollection(Enumerable.Empty<Event>());

        /// <summary>
        /// Initializes a new instance of <see cref="EventCollection"/>.
        /// </summary>
        /// <param name="events">The set of events used to populate this <see cref="EventCollection"/>.</param>
        public EventCollection(IEnumerable<Event> events)
            : base(events.AsList())
        { }
    }
}
