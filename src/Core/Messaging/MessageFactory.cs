using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

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

namespace Spark.Messaging
{
    /// <summary>
    /// Base message factory implementation.
    /// </summary>
    public abstract class MessageFactory : ICreateMessages
    {
        private static readonly String HostServer = GetHostServerName();

        /// <summary>
        /// Initializes a new instance of <see cref="MessageFactory"/>.
        /// </summary>
        internal MessageFactory()
        { }

        /// <summary>
        /// Creates a new message with a payload of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="headers">The message headers.</param>
        /// <param name="payload">The message payload.</param>
        public Message<T> Create<T>(IEnumerable<Header> headers, T payload)
        {
            return new Message<T>(GuidStrategy.NewGuid(), new HeaderCollection(CreateHeaderDictionary(headers)), payload);
        }

        /// <summary>
        /// Creates the underlying <see cref="HeaderCollection"/> dictionary.
        /// </summary>
        /// <param name="headers">The set of custom message headers.</param>
        protected virtual Dictionary<String, String> CreateHeaderDictionary(IEnumerable<Header> headers)
        {
            var result = headers == null ? new Dictionary<String, String>() : headers.ToDictionary(header => header.Name, header => header.Value);

            if (!result.ContainsKey(Header.Origin))
                result[Header.Origin] = HostServer;

            if (!result.ContainsKey(Header.Timestamp))
                result[Header.Timestamp] = SystemTime.GetTimestamp().ToString(DateTimeFormat.RoundTrip);

            return result;
        }

        /// <summary>
        /// Gets this server's fully qualified domain name.
        /// </summary>
        private static String GetHostServerName()
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            return properties.DomainName.IsNullOrWhiteSpace() ? properties.HostName : String.Format("{0}.{1}", properties.HostName, properties.DomainName);
        }
    }
}
