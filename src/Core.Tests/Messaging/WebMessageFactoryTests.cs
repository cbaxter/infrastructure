using System;
using System.Collections.Specialized;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Web;
using Moq;
using Spark.Infrastructure.Messaging;
using Spark.Infrastructure.Resources;
using Xunit;
using Xunit.Extensions;

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

namespace Spark.Infrastructure.Tests.Messaging
{
    public static class UsingWebMessageFactory
    {
        public class WhenCreatingNewMessages
        {
            [Fact]
            public void HttpContextNotRequiredIfHeadersAlreadySet()
            {
                var messageFactory = new WebMessageFactory();

                Assert.DoesNotThrow(() => messageFactory.Create(new Object(), new[]
                    {
                        new Header(Header.UserName, "nbawdy@sparksoftware.net", checkReservedNames: false), 
                        new Header(Header.RemoteAddress, IPAddress.Loopback, checkReservedNames: false), 
                        new Header(Header.UserAddress, IPAddress.Loopback, checkReservedNames: false)

                    }));
            }

            [Fact]
            public void HttpContextRequiredIfWebHeadersNotSet()
            {
                var messageFactory = new WebMessageFactory();
                var ex = Assert.Throws<InvalidOperationException>(() => messageFactory.Create(new Object(), HeaderCollection.Empty));

                Assert.Equal(Exceptions.HttpContextNotAvailable, ex.Message);
            }

            [Fact]
            public void SetRemoteAddressToRemoteAddrVariableIfNotAlreadySet()
            {
                var remoteAddress = IPAddress.Loopback.ToString();
                var httpContext = CreateFakeHttpContext(Thread.CurrentPrincipal, new NameValueCollection { { "REMOTE_ADDR", remoteAddress } });
                var messageFactory = new WebMessageFactory(() => httpContext);
                var message = messageFactory.Create(new Object(), HeaderCollection.Empty);

                Assert.Equal(IPAddress.Loopback, message.Headers.GetRemoteAddress());
            }

            [Fact]
            public void DoNotSetRemoteAddressToRemoteAddrVariableIfAlreadySet()
            {
                var httpContext = CreateFakeHttpContext(Thread.CurrentPrincipal, new NameValueCollection { { "REMOTE_ADDR", IPAddress.Loopback.ToString() } });
                var messageFactory = new WebMessageFactory(() => httpContext);
                var message = messageFactory.Create(new Object(), new[] { new Header(Header.RemoteAddress, IPAddress.None, checkReservedNames: false) });

                Assert.NotEqual(IPAddress.Loopback, message.Headers.GetRemoteAddress());
            }

            [Fact]
            public void SetUserNameToHttpContextUserIfNotAlreadySet()
            {
                var httpContext = CreateFakeHttpContext(new GenericPrincipal(new GenericIdentity("nbawdy@sparksoftware.net"), new String[0]), new NameValueCollection());
                var messageFactory = new WebMessageFactory(() => httpContext);
                var message = messageFactory.Create(new Object(), HeaderCollection.Empty);

                Assert.Equal("nbawdy@sparksoftware.net", message.Headers.GetUserName());
            }

            [Fact]
            public void DoNotSetUserNameToHttpContextUserIfAlreadySet()
            {
                var httpContext = CreateFakeHttpContext(new GenericPrincipal(new GenericIdentity("nbawdy@sparksoftware.net"), new String[0]), new NameValueCollection());
                var messageFactory = new WebMessageFactory(() => httpContext);
                var message = messageFactory.Create(new Object(), new[] { new Header(Header.UserName, "nowan@sparksoftware.net", checkReservedNames: false) });

                Assert.NotEqual("nbawdy@sparksoftware.net", message.Headers.GetUserName());
            }

            [Theory, InlineData("HTTP_CLIENT_IP"), InlineData("HTTP_X_FORWARDED_FOR"), InlineData("HTTP_X_FORWARDED"), InlineData("HTTP_X_CLUSTER_CLIENT_IP"), InlineData("HTTP_FORWARDED_FOR"), InlineData("HTTP_FORWARDED")]
            public void SetUserAddressToFirstMatchingServerVariableIfAlreadySet(String serverVariable)
            {
                var httpContext = CreateFakeHttpContext(Thread.CurrentPrincipal, new NameValueCollection { { "REMOTE_ADDR", IPAddress.Broadcast.ToString() }, { serverVariable, IPAddress.Loopback.ToString() } });
                var messageFactory = new WebMessageFactory(() => httpContext);
                var message = messageFactory.Create(new Object(), HeaderCollection.Empty);

                Assert.Equal(IPAddress.Loopback, message.Headers.GetUserAddress());
            }

            private static HttpContextBase CreateFakeHttpContext(IPrincipal principal, NameValueCollection serverVariables)
            {
                var httpContext = new Mock<HttpContextBase>();
                var httpRequest = new Mock<HttpRequestBase>();

                httpContext.Setup(mock => mock.User).Returns(principal);
                httpContext.Setup(mock => mock.Request).Returns(httpRequest.Object);
                httpRequest.Setup(mock => mock.ServerVariables).Returns(serverVariables);
                httpRequest.Setup(mock => mock.UserHostAddress).Returns(serverVariables["REMOTE_ADDR"]);

                return httpContext.Object;
            }
        }
    }
}
