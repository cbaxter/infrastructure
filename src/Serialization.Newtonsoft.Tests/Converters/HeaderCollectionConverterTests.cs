using System;
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
  ""_o"": """ + headers.GetOrigin() + @""",
  ""_t"": """ + headers.GetTimestamp().ToString(DateTimeFormat.RoundTrip) + @""",
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

                SystemTime.OverrideWith(() => now);

                var headers = new ServiceMessageFactory().Create(null, new Object()).Headers;
                var bson = WriteBson(headers);

                Validate("YgAAAAJfbwAMAAAAV29ya3N0YXRpb24AAl90AB0AAAAyMDEzLTA2LTAxVDEyOjMwOjQ1LjAwMDAwMDBaAAJfcgAcAAAAZmU4MDo6MzViOTpiNzVlOmM4MDpjNTc5JTExAAA=", bson);
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
