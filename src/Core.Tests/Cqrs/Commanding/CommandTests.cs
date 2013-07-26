using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Spark.Cqrs.Commanding;
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

namespace Spark.Tests.Cqrs.Commanding
{
    public static class UsingCommand
    {
        public class WhenGettingCommandHeaders
        {
            [Fact]
            public void ReturnHeadersFromCommandContextIfNotNull()
            {
                var command = new FakeCommand();
                var headers = new HeaderCollection(Enumerable.Empty<Header>());

                using (new CommandContext(GuidStrategy.NewGuid(), headers, CommandEnvelope.Empty))
                    Assert.Same(headers, command.Headers);
            }

            [Fact]
            public void ThrowInvalidOperationExceptionIfNoCommandContext()
            {
                var command = new FakeCommand();

                Assert.Throws<InvalidOperationException>(() => command.Headers);
            }

            [Fact]
            public void CanShortCircuitAccessToOriginHeader()
            {
                var command = new FakeCommand();
                var headers = new HeaderCollection(new[] { new Header(Header.Origin, "MyOrigin", checkReservedNames: false) });

                using (new CommandContext(GuidStrategy.NewGuid(), headers, CommandEnvelope.Empty))
                    Assert.Equal("MyOrigin", command.GetOrigin());
            }

            [Fact]
            public void CanShortCircuitAccessToTimestampHeader()
            {
                var now = DateTime.UtcNow;
                var command = new FakeCommand();
                var headers = new HeaderCollection(new[] { new Header(Header.Timestamp, now.ToString(DateTimeFormat.RoundTrip), checkReservedNames: false) });

                using (new CommandContext(GuidStrategy.NewGuid(), headers, CommandEnvelope.Empty))
                    Assert.Equal(now, command.GetTimestamp());
            }

            [Fact]
            public void CanShortCircuitAccessToremoteAddressHeader()
            {
                var command = new FakeCommand();
                var headers = new HeaderCollection(new[] { new Header(Header.RemoteAddress, IPAddress.Loopback.ToString(), checkReservedNames: false) });

                using (new CommandContext(GuidStrategy.NewGuid(), headers, CommandEnvelope.Empty))
                    Assert.Equal(IPAddress.Loopback, command.GetRemoteAddress());
            }

            [Fact]
            public void CanShortCircuitAccessToUserAddressHeader()
            {
                var command = new FakeCommand();
                var headers = new HeaderCollection(new[] { new Header(Header.UserAddress, IPAddress.Loopback.ToString(), checkReservedNames: false) });

                using (new CommandContext(GuidStrategy.NewGuid(), headers, CommandEnvelope.Empty))
                    Assert.Equal(IPAddress.Loopback, command.GetUserAddress());
            }

            [Fact]
            public void CanShortCircuitAccessToUserNameHeader()
            {
                var command = new FakeCommand();
                var headers = new HeaderCollection(new[] { new Header(Header.UserName, "nbawdy@sparksoftware.net", checkReservedNames: false) });

                using (new CommandContext(GuidStrategy.NewGuid(), headers, CommandEnvelope.Empty))
                    Assert.Equal("nbawdy@sparksoftware.net", command.GetUserName());
            }
        }

        private class FakeCommand : Command
        { }
    }

}
