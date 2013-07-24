using System;
using Spark.Infrastructure.Domain;

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
    /// Identifies a specific aggregate event instance.
    /// </summary>
    public struct EventVersion : IEquatable<EventVersion>
    {
        public static readonly EventVersion Empty = new EventVersion();
        private readonly Int32 version;
        private readonly Int32 count;
        private readonly Int32 item;

        /// <summary>
        /// The <see cref="Aggregate"/> version.
        /// </summary>
        public Int32 Version { get { return version; } }

        /// <summary>
        /// The total number of events raised within the specific aggregate <see cref="Version"/>.
        /// </summary>
        public Int32 Count { get { return count; } }

        /// <summary>
        /// The event oridinal within the specific aggregate <see cref="Version"/>.
        /// </summary>
        public Int32 Item { get { return item; } }

        /// <summary>
        /// Initializes a new instance of <see cref="EventVersion"/>.
        /// </summary>
        /// <param name="version">The <see cref="Aggregate"/> version.</param>
        /// <param name="count">The total number of events raised within the specific aggregate <see cref="Version"/></param>
        /// <param name="item">The event oridinal within the specific aggregate <see cref="Version"/>.</param>
        public EventVersion(Int32 version, Int32 count, Int32 item)
        {
            Verify.GreaterThan(0, version, "version");
            Verify.GreaterThanOrEqual(0, count, "count");
            Verify.GreaterThanOrEqual(0, item, "item");
            Verify.LessThanOrEqual(count, item, "item");

            this.version = version;
            this.count = count;
            this.item = item;
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="Object"/> are equal.
        /// </summary>
        /// <param name="other">Another object to compare.</param>
        public override Boolean Equals(Object other)
        {
            return other is EventVersion && Equals((EventVersion)other);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="EventVersion"/> are equal.
        /// </summary>
        /// <param name="other">Another <see cref="EventVersion"/> to compare.</param>
        public Boolean Equals(EventVersion other)
        {
            return version == other.version && count == other.count && item == other.item;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {
                var hash = 43;

                hash = (hash * 397) + Version.GetHashCode();
                hash = (hash * 397) + Count.GetHashCode();
                hash = (hash * 397) + Item.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns the page description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} (Event {1} of {2})", Version, Item, Count);
        }
    }
}
