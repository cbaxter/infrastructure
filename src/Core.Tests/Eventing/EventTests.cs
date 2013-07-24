using System;
using System.Linq;
using System.Net;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Messaging;
using Xunit;

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

namespace Spark.Infrastructure.Tests.Eventing
{
    public static class UsingEvent
    {
        public class WhenGettingAggregateId
        {
            [Fact]
            public void ReturnIdFromEventContextIfNotNull()
            {
                var e = new FakeEvent();
                var aggregateId = GuidStrategy.NewGuid();

                using (new EventContext(aggregateId, HeaderCollection.Empty, e))
                    Assert.Equal(aggregateId, e.FakeId);
            }

            [Fact]
            public void ThrowInvalidOperationExceptionIfNoEventContext()
            {
                var e = new FakeEvent();

                Assert.Throws<InvalidOperationException>(() => e.FakeId);
            }
        }

        public class WhenGettingEventHeaders
        {
            [Fact]
            public void ReturnHeadersFromEventContextIfNotNull()
            {
                var e = new FakeEvent();
                var headers = new HeaderCollection(Enumerable.Empty<Header>());

                using (new EventContext(GuidStrategy.NewGuid(), headers, e))
                    Assert.Same(headers, e.Headers);
            }

            [Fact]
            public void ThrowInvalidOperationExceptionIfNoEventContext()
            {
                var e = new FakeEvent();

                Assert.Throws<InvalidOperationException>(() => e.Headers);
            }

            [Fact]
            public void CanShortCircuitAccessToOriginHeader()
            {
                var e = new FakeEvent();
                var headers = new HeaderCollection(new[] { new Header(Header.Origin, "MyOrigin", checkReservedNames: false) });

                using (new EventContext(GuidStrategy.NewGuid(), headers, e))
                    Assert.Equal("MyOrigin", e.GetOrigin());
            }

            [Fact]
            public void CanShortCircuitAccessToTimestampHeader()
            {
                var now = DateTime.UtcNow;
                var e = new FakeEvent();
                var headers = new HeaderCollection(new[] { new Header(Header.Timestamp, now.ToString(DateTimeFormat.RoundTrip), checkReservedNames: false) });

                using (new EventContext(GuidStrategy.NewGuid(), headers, e))
                    Assert.Equal(now, e.GetTimestamp());
            }

            [Fact]
            public void CanShortCircuitAccessToremoteAddressHeader()
            {
                var e = new FakeEvent();
                var headers = new HeaderCollection(new[] { new Header(Header.RemoteAddress, IPAddress.Loopback.ToString(), checkReservedNames: false) });

                using (new EventContext(GuidStrategy.NewGuid(), headers, e))
                    Assert.Equal(IPAddress.Loopback, e.GetRemoteAddress());
            }

            [Fact]
            public void CanShortCircuitAccessToUserAddressHeader()
            {
                var e = new FakeEvent();
                var headers = new HeaderCollection(new[] { new Header(Header.UserAddress, IPAddress.Loopback.ToString(), checkReservedNames: false) });

                using (new EventContext(GuidStrategy.NewGuid(), headers, e))
                    Assert.Equal(IPAddress.Loopback, e.GetUserAddress());
            }

            [Fact]
            public void CanShortCircuitAccessToUserNameHeader()
            {
                var e = new FakeEvent();
                var headers = new HeaderCollection(new[] { new Header(Header.UserName, "nbawdy@sparksoftware.net", checkReservedNames: false) });

                using (new EventContext(GuidStrategy.NewGuid(), headers, e))
                    Assert.Equal("nbawdy@sparksoftware.net", e.GetUserName());
            }
        }

        private class FakeEvent : Event
        {
            public Guid FakeId { get { return AggregateId; } }
        }
    }

}
