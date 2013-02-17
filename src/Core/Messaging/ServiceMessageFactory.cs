using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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
    /// Message factory implementation for use in console, forms and service applications.
    /// </summary>
    public sealed class ServiceMessageFactory : MessageFactory
    {
        private static readonly IPAddress HostAddress = GetHostAddress();

        /// <summary>
        /// Creates the underlying <see cref="HeaderCollection"/> dictionary.
        /// </summary>
        /// <param name="headers">The set of custom message headers.</param>
        protected override Dictionary<String, Object> CreateHeaderDictionary(IEnumerable<Header> headers)
        {
            var result = base.CreateHeaderDictionary(headers);
            
            if (!result.ContainsKey(Header.RemoteAddress))
                result[Header.RemoteAddress] = HostAddress;

            if (!result.ContainsKey(Header.UserName))
            {
                var principal = Thread.CurrentPrincipal;
                if (principal != null && principal.Identity.Name.IsNotNullOrWhiteSpace())
                    result[Header.UserName] = principal.Identity.Name;
            }

            return result;
        }

        /// <summary>
        /// Gets this server's inter-network IP address.
        /// </summary>
        private static IPAddress GetHostAddress()
        {
            return Dns.GetHostAddresses(Dns.GetHostName()).Where(IsInterNetworkAddress).OrderByDescending(ipAddress => ipAddress.AddressFamily).FirstOrDefault() ?? IPAddress.Loopback;
        }

        /// <summary>
        /// Returns true if the <paramref name="ipAddress"/> address family is <see cref="AddressFamily.InterNetwork"/> or <see cref="AddressFamily.InterNetworkV6"/>; otherwise false.
        /// </summary>
        /// <param name="ipAddress">The ip address to check address family</param>
        private static Boolean IsInterNetworkAddress(IPAddress ipAddress)
        {
            return ipAddress != null && (ipAddress.AddressFamily == AddressFamily.InterNetwork || ipAddress.AddressFamily == AddressFamily.InterNetworkV6);
        }
    }
}
