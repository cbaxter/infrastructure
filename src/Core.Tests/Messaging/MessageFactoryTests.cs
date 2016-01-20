using System;
using System.Net.NetworkInformation;
using Spark;
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

namespace Test.Spark.Messaging
{
    namespace UsingMessageFactory
    {
        public class WhenCreatingNewMessages
        {
            [Fact]
            public void HeadersCanBeNull()
            {
                var messageFactory = new FakeMessageFactory();

                Assert.NotNull(messageFactory.Create(null, new Object()));
            }

            [Fact]
            public void SetOriginToHostServerNameIfNotAlreadySet()
            {
                var messageFactory = new FakeMessageFactory();
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                var message = messageFactory.Create(HeaderCollection.Empty, new Object());
                var serverName = properties.DomainName.IsNullOrWhiteSpace() ? properties.HostName : $"{properties.HostName}.{properties.DomainName}";

                Assert.Equal(serverName, message.Headers.GetOrigin());
            }

            [Fact]
            public void DoNotSetOriginToHostServerNameIfAlreadySet()
            {
                var messageFactory = new FakeMessageFactory();
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                var message = messageFactory.Create(new[] { new Header(Header.Origin, "NotThisMachineName", checkReservedNames: false) }, new Object());
                var serverName = properties.DomainName.IsNullOrWhiteSpace() ? properties.HostName : $"{properties.HostName}.{properties.DomainName}";

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
            public void AlwaysSetTimestamp()
            {
                var now = DateTime.UtcNow;
                var messageFactory = new FakeMessageFactory();
                var timestamp = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));

                SystemTime.OverrideWith(() => now);

                var message = messageFactory.Create(new[] { new Header(Header.Timestamp, timestamp.ToString(DateTimeFormat.RoundTrip), checkReservedNames: false) }, new Object());

                Assert.True(message.Headers.GetTimestamp() > timestamp);
            }
        }

        internal class FakeMessageFactory : MessageFactory
        { }
    }
}
