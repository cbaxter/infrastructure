using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Principal;

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
    /// A read-only collection of named values (headers).
    /// </summary>
    [Serializable]
    public sealed class HeaderCollection : ReadOnlyDictionary<String, Object>, IEnumerable<Header>
    {
        /// <summary>
        /// Represents an empty <see cref="HeaderCollection"/>. This field is read-only.
        /// </summary>
        public static readonly HeaderCollection Empty = new HeaderCollection(new Dictionary<String, Object>());

        /// <summary>
        /// Initializes a new instance of <see cref="HeaderCollection"/> with both system and custom headers.
        /// </summary>
        /// <param name="headers">The set of headers used to populate this <see cref="HeaderCollection"/>.</param>
        public HeaderCollection(IEnumerable<Header> headers)
            : this(headers == null ? new Dictionary<String, Object>() : headers.ToDictionary(header => header.Name, header => header.Value))
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="HeaderCollection"/>.
        /// </summary>
        /// <param name="dictionary">The set of named values used to populate this <see cref="HeaderCollection"/>.</param>
        internal HeaderCollection(IDictionary<String, Object> dictionary)
            : base(dictionary)
        { }

        /// <summary>
        /// Returns the origin server name or an empty string if not set.
        /// </summary>
        public String GetOrigin()
        {
            Object value;
            return TryGetValue(Header.Origin, out value) && value != null ? value.ToString() : String.Empty;
        }

        /// <summary>
        /// Returns the creation timestamp or the current system time if not set.
        /// </summary>
        public DateTime GetTimestamp()
        {
            Object value;

            if (!TryGetValue(Header.Timestamp, out value) || value == null)
                return SystemTime.Now;

            if (value is DateTime)
                return (DateTime)value;

            DateTime timestamp;
            return DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestamp) ? timestamp : SystemTime.Now;
        }

        /// <summary>
        /// Returns the sender's remote address or <see cref="IPAddress.None"/> if not set.
        /// </summary>
        public IPAddress GetRemoteAddress()
        {
            var ipAddress = GetAddress(Header.RemoteAddress);

            return ipAddress;
        }

        /// <summary>
        /// Returns the sender's client address or <see cref="IPAddress.None"/> if not set.
        /// </summary>
        /// <remarks>Value will be the same as <see cref="GetRemoteAddress"/> unless the request went through an intermediary such as a load-balancer or proxy.</remarks>
        public IPAddress GetUserAddress()
        {
            var ipAddress = GetAddress(Header.UserAddress);

            return IPAddress.None.Equals(ipAddress) ? GetRemoteAddress() : ipAddress;
        }

        /// <summary>
        /// Gets an IP Address from this <see cref="HeaderCollection"/>.
        /// </summary>
        /// <param name="name">The name of the header to be accessed.</param>
        private IPAddress GetAddress(String name)
        {
            Object value;

            if (!TryGetValue(name, out value) || value == null)
                return IPAddress.None;

            var ipAddress = value as IPAddress;
            if (ipAddress != null)
                return ipAddress;

            return IPAddress.TryParse(value.ToString(), out ipAddress) ? ipAddress : IPAddress.None;
        }

        /// <summary>
        /// Returns the <see cref="IIdentity.Name"/> of the user principal that sent the associate message, or <see cref="String.Empty"/> if not set.
        /// </summary>
        public String GetUserName()
        {
            Object value;
            return TryGetValue(Header.UserName, out value) && value != null ? value.ToString() : String.Empty;
        }
        
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="HeaderCollection"/>.
        /// </summary>
        /// <returns></returns>
        IEnumerator<Header> IEnumerable<Header>.GetEnumerator()
        {
            var headers = Dictionary.Select(kvp => new Header(kvp.Key, kvp.Value, checkReservedNames: false)).ToList();

            return headers.GetEnumerator();
        }
    }
}
