using System;
using System.Collections.Generic;
using System.Net;
using Spark;
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

namespace Test.Spark.Messaging
{
    namespace UsingHeaderCollection
    {
        public class WhenReferencingEmptyHeaders
        {
            [Fact]
            public void AlwaysReturnSameInstance()
            {
                Assert.Same(HeaderCollection.Empty, HeaderCollection.Empty);
            }
        }

        public class WhenCreatingHeaderCollection
        {
            [Fact]
            public void CanMutateUnderlyingCollection()
            {
                var dictionary = new Dictionary<String, String>();
                var headers = new HeaderCollection(dictionary);

                dictionary.Add("MyTestKey", "MyTestValue");

                Assert.Equal(1, headers.Count);
            }

            [Fact]
            public void CanCloneHeaderCollection()
            {
                var dictionary = new Dictionary<String, String>();
                var headers = (IEnumerable<Header>)new HeaderCollection(dictionary);

                dictionary.Add("MyTestKey", "MyTestValue");

                Assert.Equal(1, new HeaderCollection(headers).Count);
            }
        }

        public class WhenGettingOrigin
        {
            [Fact]
            public void ReturnEmptyStringIfNotSpecified()
            {
                var headers = HeaderCollection.Empty;

                Assert.Equal(String.Empty, headers.GetOrigin());
            }

            [Fact]
            public void ReturnHeaderValueIfSpecified()
            {
                var header = new Header(Header.Origin, "ServerName", checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());

                Assert.Equal("ServerName", headers.GetOrigin());
            }
        }

        public class WhenGettingTimestamp : IDisposable
        {
            public void Dispose()
            {
                SystemTime.ClearOverride();
            }

            [Fact]
            public void ReturnCurrentSystemTimeIfNotSpecified()
            {
                var headers = HeaderCollection.Empty;
                var now = DateTime.UtcNow;

                SystemTime.OverrideWith(() => now);
                Assert.InRange(headers.GetTimestamp().Ticks, now.Ticks - 10, now.Ticks + 10);
            }

            [Fact]
            public void ReturnCurrentSystemTimeIfHeaderValueNull()
            {
                var header = new Header(Header.Timestamp, null, checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());
                var now = DateTime.UtcNow;

                SystemTime.OverrideWith(() => now);
                Assert.InRange(headers.GetTimestamp().Ticks, now.Ticks - 10, now.Ticks + 10);
            }

            [Fact]
            public void ReturnParsedDateTimeIfValueIsNotDateTime()
            {
                var now = new DateTime(2013, 2, 10, 15, 58, 12);
                var header = new Header(Header.Timestamp, now.ToString(DateTimeFormat.RoundTrip), checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());

                Assert.InRange(headers.GetTimestamp().Ticks, now.Ticks - 10, now.Ticks + 10);
            }

            [Fact]
            public void ReturnCurrentSystemTimeIfHeaderValueCannotBeParsed()
            {
                var header = new Header(Header.Timestamp, "ServerName", checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());
                var now = DateTime.UtcNow;

                SystemTime.OverrideWith(() => now);
                Assert.InRange(headers.GetTimestamp().Ticks, now.Ticks - 10, now.Ticks + 10);
            }
        }

        public class WhenGettingRemoteAddress
        {
            [Fact]
            public void ReturnNoneIfNotSpecified()
            {
                var headers = HeaderCollection.Empty;

                Assert.Equal(IPAddress.None, headers.GetRemoteAddress());
            }

            [Fact]
            public void ReturnNoneIfHeaderValueNull()
            {
                var header = new Header(Header.RemoteAddress, null, checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());

                Assert.Equal(IPAddress.None, headers.GetRemoteAddress());
            }

            [Fact]
            public void ReturnParsedAddressIfValueIsNotAddress()
            {
                var header = new Header(Header.RemoteAddress, IPAddress.Loopback.ToString(), checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());

                Assert.Equal(IPAddress.Loopback, headers.GetRemoteAddress());
            }

            [Fact]
            public void ReturnNoneeIfHeaderValueCannotBeParsed()
            {
                var header = new Header(Header.RemoteAddress, "ServerName", checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());

                Assert.Equal(IPAddress.None, headers.GetRemoteAddress());
            }
        }

        public class WhenGettingUserAddress
        {
            [Fact]
            public void ReturnNoneIfNotSpecified()
            {
                var headers = HeaderCollection.Empty;

                Assert.Equal(IPAddress.None, headers.GetUserAddress());
            }

            [Fact]
            public void ReturnNoneIfHeaderValueNull()
            {
                var header = new Header(Header.UserAddress, null, checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());

                Assert.Equal(IPAddress.None, headers.GetUserAddress());
            }

            [Fact]
            public void ReturnParsedAddressIfValueIsNotAddress()
            {
                var header = new Header(Header.UserAddress, IPAddress.Loopback.ToString(), checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());

                Assert.Equal(IPAddress.Loopback, headers.GetUserAddress());
            }

            [Fact]
            public void ReturnNoneeIfHeaderValueCannotBeParsed()
            {
                var header = new Header(Header.UserAddress, "ServerName", checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());

                Assert.Equal(IPAddress.None, headers.GetUserAddress());
            }

            [Fact]
            public void ReturnRemoteAddressIfUserAddressNotSpecified()
            {
                var header = new Header(Header.RemoteAddress, IPAddress.Loopback.ToString(), checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());

                Assert.Equal(IPAddress.Loopback, headers.GetUserAddress());
            }
        }

        public class WhenGettingUserName
        {
            [Fact]
            public void ReturnEmptyStringIfNotSpecified()
            {
                var headers = HeaderCollection.Empty;

                Assert.Equal(String.Empty, headers.GetUserName());
            }

            [Fact]
            public void ReturnHeaderValueIfSpecified()
            {
                var header = new Header(Header.UserName, "nbawdy@sparksoftware.net", checkReservedNames: false);
                var headers = new HeaderCollection(header.ToEnumerable());

                Assert.Equal("nbawdy@sparksoftware.net", headers.GetUserName());
            }
        }
    }
}
