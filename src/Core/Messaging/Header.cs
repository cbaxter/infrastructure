using System;
using System.Collections.Generic;
using Spark.Infrastructure.Resources;

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

namespace Spark.Infrastructure.Messaging
{
    /// <summary>
    /// Represents a named message header.
    /// </summary>
    public struct Header : IEquatable<Header>
    {
        public const String Aggregate = "_a";
        public const String Origin = "_o";
        public const String Timestamp = "_t";
        public const String RemoteAddress = "_r";
        public const String UserAddress = "_c";
        public const String UserName = "_i";

        private static readonly HashSet<String> ReservedNames = new HashSet<String> { Origin, Timestamp, RemoteAddress, UserAddress, UserName };
        private readonly String value;
        private readonly String name;

        /// <summary>
        /// The name of the header value.
        /// </summary>
        public String Name { get { return name; } }

        /// <summary>
        /// The header value.
        /// </summary>
        public String Value { get { return value; } }

        /// <summary>
        /// Initializes a new instance of <see cref="Header"/>.
        /// </summary>
        /// <param name="name">The name of the header value.</param>
        /// <param name="value">The header value.</param>
        public Header(String name, String value)
            : this(name, value, true)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="Header"/> by-passing reserved name check.
        /// </summary>
        internal Header(String name, String value, Boolean checkReservedNames)
        {
            Verify.NotNullOrWhiteSpace(name, "name");
            Verify.False(checkReservedNames && ReservedNames.Contains(name), "name", Exceptions.ReservedHeaderName.FormatWith(name));

            this.name = name;
            this.value = value;
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="Object"/> are equal.
        /// </summary>
        /// <param name="other">Another object to compare.</param>
        public override Boolean Equals(Object other)
        {
            return other is Header && Equals((Header)other);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="Header"/> are equal.
        /// </summary>
        /// <param name="other">Another <see cref="Header"/> to compare.</param>
        public Boolean Equals(Header other)
        {
            return other.Name == Name && Equals(other.Value, Value);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 43;

                hash = (hash * 397) + (Name ?? String.Empty).GetHashCode();
                hash = (hash * 397) + (Value ?? String.Empty).GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns the description for this instance.
        /// </summary>
        public override string ToString()
        {
            return String.Format("[{0},{1}]", name, value);
        }
    }
}
