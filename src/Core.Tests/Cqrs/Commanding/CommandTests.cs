using System;
using System.Linq;
using System.Net;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Messaging;
using Xunit;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Test.Spark.Cqrs.Commanding
{
    namespace UsingCommand
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

        internal class FakeCommand : Command
        { }
    }

}
