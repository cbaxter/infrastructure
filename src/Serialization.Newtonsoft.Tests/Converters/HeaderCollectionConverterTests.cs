using System;
using System.Net;
using Spark;
using Spark.Messaging;
using Spark.Serialization.Converters;
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
    public static class UsingHeaderCollectionConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(new HeaderCollectionConverter(), default(HeaderCollection));

                Validate("null", json);
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var headers = new ServiceMessageFactory().Create(null, new Object()).Headers;
                var json = WriteJson(new HeaderCollectionConverter(), headers);

                Validate(
                    String.Format("{{\"_o\":\"{0}\",\"_t\":\"{1}\",\"_r\":\"{2}\"}}", headers.GetOrigin(), headers.GetTimestamp().ToString(DateTimeFormat.RoundTrip), headers.GetRemoteAddress()),
                    json
                );
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<HeaderCollection>(new HeaderCollectionConverter(), "null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var json = "﻿{\"_o\":\"Workstation\",\"_t\":\"2013-06-02T18:23:59.7237080Z\",\"_r\":\"fe80::54c:c8d:e628:6ad6%12\"}";
                var headers = ReadJson<HeaderCollection>(new HeaderCollectionConverter(), json);

                Assert.Equal("Workstation", headers.GetOrigin());
            }

            [Fact]
            public void WillIgnoreNullHeaderValues()
            {
                var json = "﻿{\"_o\":\"Workstation\",\"_t\":\"2013-06-02T18:23:59.7237080Z\",\"_r\":null}";
                var headers = ReadJson<HeaderCollection>(new HeaderCollectionConverter(), json);

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
                var bson = WriteBson(new HeaderCollectionConverter(), headers);

                Validate("﻿YQAAAAJfbwAMAAAAV29ya3N0YXRpb24AAl90AB0AAAAyMDEzLTA2LTAxVDEyOjMwOjQ1LjAwMDAwMDBaAAJfcgAbAAAAZmU4MDo6NTRjOmM4ZDplNjI4OjZhZDYlMTIAAA==", bson);
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var now = new DateTime(2013, 6, 1, 12, 30, 45, DateTimeKind.Utc);
                var bson = "﻿YQAAAAJfbwAMAAAAV29ya3N0YXRpb24AAl90AB0AAAAyMDEzLTA2LTAxVDEyOjMwOjQ1LjAwMDAwMDBaAAJfcgAbAAAAZmU4MDo6NTRjOmM4ZDplNjI4OjZhZDYlMTIAAA==";
                var headers = ReadBson<HeaderCollection>(new HeaderCollectionConverter(), bson);

                Assert.Equal(now, headers.GetTimestamp());
            }
        }
    }
}
