using System;
using System.Linq;
using System.Net;
using Spark.Infrastructure.Commanding;
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

namespace Spark.Infrastructure.Tests.Commanding
{
    public static class UsingCommand
    {
        public class WhenCreatingCommand
        {
            [Fact]
            public void MustMapAggregateId()
            {
                var fakeId = Guid.NewGuid();
                var command = new FakeCommand(fakeId);

                Assert.Equal(fakeId, command.AggregateId);
            }
        }

        public class WhenGettingCommandHeaders
        {
            [Fact]
            public void ReturnHeadersFromCommandContextIfNotNull()
            {
                var headers = new HeaderCollection(Enumerable.Empty<Header>());
                var command = new FakeCommand(Guid.NewGuid());

                using (new CommandContext(Guid.NewGuid(), headers))
                    Assert.Same(headers, command.Headers);
            }

            [Fact]
            public void ReturnEmptyHeaderCollectionIfNoCommandContext()
            {
                var command = new FakeCommand(Guid.NewGuid());

                Assert.Same(HeaderCollection.Empty, command.Headers);
            }

            [Fact]
            public void CanShortCircuitAccessToOriginHeader()
            {
                var headers = new HeaderCollection(new[] { new Header(Header.Origin, "MyOrigin", checkReservedNames: false) });
                var command = new FakeCommand(Guid.NewGuid());

                using (new CommandContext(Guid.NewGuid(), headers))
                    Assert.Equal("MyOrigin", command.GetOrigin());
            }

            [Fact]
            public void CanShortCircuitAccessToTimestampHeader()
            {
                var now = DateTime.UtcNow;
                var headers = new HeaderCollection(new[] { new Header(Header.Timestamp, now, checkReservedNames: false) });
                var command = new FakeCommand(Guid.NewGuid());

                using (new CommandContext(Guid.NewGuid(), headers))
                    Assert.Equal(now, command.GetTimestamp());
            }

            [Fact]
            public void CanShortCircuitAccessToremoteAddressHeader()
            {
                var headers = new HeaderCollection(new[] { new Header(Header.RemoteAddress, IPAddress.Loopback, checkReservedNames: false) });
                var command = new FakeCommand(Guid.NewGuid());

                using (new CommandContext(Guid.NewGuid(), headers))
                    Assert.Equal(IPAddress.Loopback, command.GetRemoteAddress());
            }

            [Fact]
            public void CanShortCircuitAccessToUserAddressHeader()
            {
                var headers = new HeaderCollection(new[] { new Header(Header.UserAddress, IPAddress.Loopback, checkReservedNames: false) });
                var command = new FakeCommand(Guid.NewGuid());

                using (new CommandContext(Guid.NewGuid(), headers))
                    Assert.Equal(IPAddress.Loopback, command.GetUserAddress());
            }

            [Fact]
            public void CanShortCircuitAccessToUserNameHeader()
            {
                var headers = new HeaderCollection(new[] { new Header(Header.UserName, "nbawdy@sparksoftware.net", checkReservedNames: false) });
                var command = new FakeCommand(Guid.NewGuid());

                using (new CommandContext(Guid.NewGuid(), headers))
                    Assert.Equal("nbawdy@sparksoftware.net", command.GetUserName());
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnFriendlyDescription()
            {
                var fakeId = Guid.NewGuid();
                var command = new FakeCommand(fakeId);

                Assert.Equal(String.Format("{0} - {1}", typeof(FakeCommand), fakeId), command.ToString());
            }
        }

        private class FakeCommand : Command
        {
            private readonly Guid fakeId;

            private Guid FakeId { get { return fakeId; } }

            public FakeCommand(Guid fakeId)
            {
                this.fakeId = fakeId;
            }

            protected override Guid GetAggregateId()
            {
                return FakeId;
            }
        }
    }

}
