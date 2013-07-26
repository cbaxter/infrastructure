using System;
using System.Net.NetworkInformation;
using Spark.Messaging;
using Xunit;

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

namespace Spark.Tests.Messaging
{
    public static class UsingMessageFactory
    {
        public class WhenCreatingNewMessages
        {
            [Fact]
            public void HeadersCanBeNull()
            {
                var messageFactory = new FakeMessageFactory();

                Assert.DoesNotThrow(() => messageFactory.Create(null, new Object()));
            }

            [Fact]
            public void SetOriginToHostServerNameIfNotAlreadySet()
            {
                var messageFactory = new FakeMessageFactory();
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                var message = messageFactory.Create(HeaderCollection.Empty, new Object());
                var serverName = properties.DomainName.IsNullOrWhiteSpace() ? properties.HostName : String.Format("{0}.{1}", properties.HostName, properties.DomainName);

                Assert.Equal(serverName, message.Headers.GetOrigin());
            }

            [Fact]
            public void DoNotSetOriginToHostServerNameIfAlreadySet()
            {
                var messageFactory = new FakeMessageFactory();
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                var message = messageFactory.Create(new[] { new Header(Header.Origin, "NotThisMachineName", checkReservedNames: false) }, new Object());
                var serverName = properties.DomainName.IsNullOrWhiteSpace() ? properties.HostName : String.Format("{0}.{1}", properties.HostName, properties.DomainName);

                Assert.NotEqual(serverName, message.Headers.GetOrigin());
            }

            [Fact]
            public void SetTimestampToSystemTimeIfNotAlreadySet()
            {
                var now = DateTime.UtcNow;
                var messageFactory = new FakeMessageFactory();

                SystemTime.OverrideWith(() => now);

                var message = messageFactory.Create(HeaderCollection.Empty, new Object());

                Assert.Equal(now.ToString(DateTimeFormat.RFC1123), message.Headers.GetTimestamp().ToString(DateTimeFormat.RFC1123));
            }

            [Fact]
            public void DoNotSetTimestampIfAlreadySet()
            {
                var now = DateTime.UtcNow;
                var messageFactory = new FakeMessageFactory();
                var timestamp = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));

                SystemTime.OverrideWith(() => now);

                var message = messageFactory.Create(new[] { new Header(Header.Timestamp, timestamp.ToString(DateTimeFormat.RoundTrip), checkReservedNames: false) }, new Object());

                Assert.Equal(timestamp, message.Headers.GetTimestamp());
            }
        }

        private class FakeMessageFactory : MessageFactory
        { }
    }
}
