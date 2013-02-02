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

namespace Spark.Infrastructure.EventStore
{
    /// <summary>
    /// A record range representing a data page.
    /// </summary>
    public struct Page : IEquatable<Page>
    {
        private readonly Int32 skip;
        private readonly Int32 take;

        /// <summary>
        /// The number of records to skip to reach the first record of this page.
        /// </summary>
        public Int32 Skip { get { return skip; } }

        /// <summary>
        /// The number of records required to fill this page (page size).
        /// </summary>
        public Int32 Take { get { return take; } }

        /// <summary>
        /// Initializes a new <see cref="Page"/>.
        /// </summary>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="take">The number of records to take.</param>
        public Page(Int32 skip, Int32 take)
        {
            Verify.GreaterThanOrEqual(0, skip, "skip");
            Verify.GreaterThan(0, take, "take");

            this.skip = skip;
            this.take = take;
        }

        /// <summary>
        /// Advances to the next page.
        /// </summary>
        /// <returns></returns>
        public Page NextPage()
        {
            return new Page(skip + take, take);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="Object"/> are equal.
        /// </summary>
        /// <param name="other">Another object to compare.</param>
        public override Boolean Equals(Object other)
        {
            return other is Page && Equals((Page)other);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="Page"/> are equal.
        /// </summary>
        /// <param name="other">Another <see cref="Page"/> to compare.</param>
        public Boolean Equals(Page other)
        {
            return other.Skip == Skip && other.Take == Take;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {
                var hash = 43;

                hash = (hash * 397) + Skip.GetHashCode();
                hash = (hash * 397) + Take.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns the page description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} - {1}", skip, take);
        }
    }
}
