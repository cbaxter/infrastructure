using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.Remoting.Channels;
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

namespace Test.Spark.Serialization.Converters
{
    namespace UsingHeaderCollectionConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(default(HeaderCollection));

                Validate(json, "null");
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var headers = new ServiceMessageFactory().Create(null, new Object()).Headers;
                var json = WriteJson(headers);

                Validate(json, @"
{
  ""_t"": """ + headers.GetTimestamp().ToString(DateTimeFormat.RoundTrip) + @""",
  ""_o"": """ + headers.GetOrigin() + @""",
  ""_r"": """ + headers.GetRemoteAddress() + @"""
}");
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<HeaderCollection>("null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var headers = ReadJson<HeaderCollection>(@"
{
  ""_o"": ""Workstation"",
  ""_t"": ""2013-06-02T18:23:59.7237080Z"",
  ""_r"": ""fe80::54c:c8d:e628:6ad6%12""
}");
                Assert.Equal("Workstation", headers.GetOrigin());
            }

            [Fact]
            public void WillIgnoreNullHeaderValues()
            {
                var headers = ReadJson<HeaderCollection>(@"
{
  ""_o"": ""Workstation"",
  ""_t"": ""2013-06-02T18:23:59.7237080Z"",
  ""_r"": null
}");

                Assert.Equal(IPAddress.None, headers.GetRemoteAddress());
            }
        }

        public class WhenWritingBson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeToBson()
            {
                var now = new DateTime(2013, 6, 1, 12, 30, 45, DateTimeKind.Utc);
                var headers = new HeaderCollection(new Dictionary<String, String>
                {
                    { Header.Origin, "serverName" },
                    { Header.Timestamp, now.ToString(CultureInfo.InvariantCulture) },
                    { Header.RemoteAddress, "127.0.0.1" },
                    { Header.UserAddress, "192.168.1.1" },
                    { Header.UserName, "testUser" }
                });
                
                var bson = WriteBson(headers);

                Validate(bson, "awAAAAJfbwALAAAAc2VydmVyTmFtZQACX3QAFAAAADA2LzAxLzIwMTMgMTI6MzA6NDUAAl9yAAoAAAAxMjcuMC4wLjEAAl9jAAwAAAAxOTIuMTY4LjEuMQACX2kACQAAAHRlc3RVc2VyAAA=");
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var now = new DateTime(2013, 6, 1, 12, 30, 45, DateTimeKind.Utc);
                var bson = "﻿YQAAAAJfbwAMAAAAV29ya3N0YXRpb24AAl90AB0AAAAyMDEzLTA2LTAxVDEyOjMwOjQ1LjAwMDAwMDBaAAJfcgAbAAAAZmU4MDo6NTRjOmM4ZDplNjI4OjZhZDYlMTIAAA==";
                var headers = ReadBson<HeaderCollection>(bson);

                Assert.Equal(now, headers.GetTimestamp());
            }
        }
    }
}
