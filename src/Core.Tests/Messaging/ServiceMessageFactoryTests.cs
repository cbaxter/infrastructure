using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading;
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
    namespace UsingServiceMessageFactory
    {
        public class WhenCreatingNewMessages
        {
            [Fact]
            public void SetRemoteAddressToHostServerNameIfNotAlreadySet()
            {
                var hostAddress = GetHostAddress();
                var messageFactory = new ServiceMessageFactory();
                var message = messageFactory.Create(HeaderCollection.Empty, new Object());

                Assert.Equal(hostAddress, message.Headers.GetRemoteAddress());
            }

            [Fact]
            public void DoNotSetOriginToHostServerNameIfAlreadySet()
            {
                var hostAddress = GetHostAddress();
                var messageFactory = new ServiceMessageFactory();
                var message = messageFactory.Create(new[] { new Header(Header.RemoteAddress, IPAddress.None.ToString(), checkReservedNames: false) }, new Object());

                Assert.NotEqual(hostAddress, message.Headers.GetRemoteAddress());
            }
            [Fact]
            public void SetUserNameToThreadPrincipalIdentityNameIfNotAlreadySet()
            {
                Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("nbawdy@sparksoftware.net"), new String[0]);

                var messageFactory = new ServiceMessageFactory();
                var message = messageFactory.Create(HeaderCollection.Empty, new Object());

                Assert.Equal("nbawdy@sparksoftware.net", message.Headers.GetUserName());
            }

            [Fact]
            public void DoNotSetUserNameToThreadPrincipalIdentityNameIfAlreadySet()
            {
                Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("nbawdy@sparksoftware.net"), new String[0]);

                var messageFactory = new ServiceMessageFactory();
                var message = messageFactory.Create(new[] { new Header(Header.UserName, "nowan@sparksoftware.net", checkReservedNames: false) }, new Object());

                Assert.NotEqual("nbawdy@sparksoftware.net", message.Headers.GetUserName());
            }

            [Fact]
            public void DoNotSetUserNameIfThreadPrincipalIsNull()
            {
                Thread.CurrentPrincipal = null;

                var messageFactory = new ServiceMessageFactory();
                var message = messageFactory.Create(HeaderCollection.Empty, new Object());

                Assert.Equal(String.Empty, message.Headers.GetUserName());
            }

            private static IPAddress GetHostAddress()
            {
                return Dns.GetHostAddresses(Dns.GetHostName())
                          .Where(ipAddress => ipAddress != null && (ipAddress.AddressFamily == AddressFamily.InterNetwork || ipAddress.AddressFamily == AddressFamily.InterNetworkV6))
                          .OrderByDescending(ipAddress => ipAddress.AddressFamily)
                          .FirstOrDefault() ?? IPAddress.Loopback;
            }
        }
    }
}
