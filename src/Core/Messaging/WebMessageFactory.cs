using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Spark.Resources;

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

namespace Spark.Messaging
{
    /// <summary>
    /// Message factory implementation for use in web applications.
    /// </summary>
    /// <remarks>
    /// An <exception cref="InvalidOperationException">InvalidOperationException</exception> will be thrown if a header must be resolved
    /// and an HTTP context is not available on the current thread. Either create the message on a thread with access to the current HTTP
    /// context or ensure that the headers are copied over from an existing message where the headers have already been set.
    /// </remarks>
    public sealed class WebMessageFactory : MessageFactory
    {
        private readonly Func<HttpContextBase> httpContextResolver;

        /// <summary>
        /// Initializes a new instance of <see cref="WebMessageFactory"/>.
        /// </summary>
        public WebMessageFactory()
            : this(GetCurrentHttpContext)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="WebMessageFactory"/> with the specified <paramref name="httpContextResolver"/>.
        /// </summary>
        /// <param name="httpContextResolver">The HTTP context resolver.</param>
        internal WebMessageFactory(Func<HttpContextBase> httpContextResolver)
        {
            Verify.NotNull(httpContextResolver, "httpContextResolver");

            this.httpContextResolver = httpContextResolver;
        }

        /// <summary>
        /// Gets the current HTTP context if available; otherwise null.
        /// </summary>
        private static HttpContextWrapper GetCurrentHttpContext()
        {
            var httpContext = HttpContext.Current;

            return httpContext == null ? null : new HttpContextWrapper(httpContext);
        }

        /// <summary>
        /// Creates the underlying <see cref="HeaderCollection"/> dictionary.
        /// </summary>
        /// <param name="headers">The set of custom message headers.</param>
        protected override Dictionary<String, String> CreateHeaderDictionary(IEnumerable<Header> headers)
        {
            var result = base.CreateHeaderDictionary(headers);

            if (!result.ContainsKey(Header.RemoteAddress))
                result[Header.RemoteAddress] = GetRemoteAddress().ToString();

            if (!result.ContainsKey(Header.UserAddress))
            {
                var ipAddress = GetUserAddress();
                if (ipAddress != null)
                    result[Header.UserAddress] = ipAddress.ToString();
            }

            if (!result.ContainsKey(Header.UserName))
            {
                var httpContext = GetHttpContext();
                if (httpContext.User != null && httpContext.User.Identity.Name.IsNotNullOrWhiteSpace())
                    result[Header.UserName] = httpContext.User.Identity.Name;
            }

            return result;
        }

        /// <summary>
        /// Gets the remote address associated with the current HTTP request.
        /// </summary>
        private IPAddress GetRemoteAddress()
        {
            var httpRequest = GetHttpRequest();

            IPAddress ipAddress;
            return IPAddress.TryParse(httpRequest.UserHostAddress ?? String.Empty, out ipAddress) ? ipAddress : IPAddress.None;
        }

        /// <summary>
        /// Gets the user's client IP address.
        /// </summary>
        /// <remarks>
        /// Implementation based on http://stackoverflow.com/questions/1634782/what-is-the-most-accurate-way-to-retrieve-a-users-correct-ip-address-in-php.
        /// </remarks>
        private IPAddress GetUserAddress()
        {
            HttpRequestBase httpRequest = GetHttpRequest();
            IPAddress ipAddress;
            String rawValue;

            rawValue = httpRequest.ServerVariables["HTTP_CLIENT_IP"];
            if (rawValue != null && IPAddress.TryParse(rawValue, out ipAddress))
                return ipAddress;

            rawValue = httpRequest.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (rawValue != null && IPAddress.TryParse((rawValue.Split(',').FirstOrDefault(value => value.IsNotNullOrWhiteSpace()) ?? String.Empty), out ipAddress))
                return ipAddress;

            rawValue = httpRequest.ServerVariables["HTTP_X_FORWARDED"];
            if (rawValue != null && IPAddress.TryParse(rawValue, out ipAddress))
                return ipAddress;

            rawValue = httpRequest.ServerVariables["HTTP_X_CLUSTER_CLIENT_IP"];
            if (rawValue != null && IPAddress.TryParse(rawValue, out ipAddress))
                return ipAddress;

            rawValue = httpRequest.ServerVariables["HTTP_FORWARDED_FOR"];
            if (rawValue != null && IPAddress.TryParse(rawValue, out ipAddress))
                return ipAddress;

            rawValue = httpRequest.ServerVariables["HTTP_FORWARDED"];
            if (rawValue != null && IPAddress.TryParse(rawValue, out ipAddress))
                return ipAddress;

            return null;
        }

        /// <summary>
        /// Gets the underlying HTTP context if avalable; otherwise throws an <exception cref="InvalidOperationException">InvalidOperationException</exception>.
        /// </summary>
        /// <returns></returns>
        private HttpContextBase GetHttpContext()
        {
            var httpContext = httpContextResolver.Invoke();
            if (httpContext == null)
                throw new InvalidOperationException(Exceptions.HttpContextNotAvailable);

            return httpContext;
        }

        /// <summary>
        /// Gets the underlying HTTP request.
        /// </summary>
        private HttpRequestBase GetHttpRequest()
        {
            return GetHttpContext().Request;
        }
    }
}
