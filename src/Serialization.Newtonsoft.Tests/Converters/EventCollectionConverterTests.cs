using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Serialization.Converters;
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

namespace Spark.Infrastructure.Serialization.Tests.Converters
{
    public static class UsingEventCollectionConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(new EventCollectionConverter(), default(EventCollection));

                Validate("null", json);
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var events = new EventCollection(new[] { new FakeEvent("My Property") });
                var json = WriteJson(new EventCollectionConverter(), events);

                Validate("[{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingEventCollectionConverter+FakeEvent, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"Property\":\"My Property\"}]", json);
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<EventCollection>(new EventCollectionConverter(), "null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var json = "﻿[{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingEventCollectionConverter+FakeEvent, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"Property\":\"My Property\"}]";
                var events = ReadJson<EventCollection>(new EventCollectionConverter(), json);

                Assert.Equal("My Property", events.OfType<FakeEvent>().Single().Property);
            }
        }

        public class WhenWritingBson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeToBson()
            {
                var events = new EventCollection(new[] { new FakeEvent("My Property") });
                var bson = WriteBson(new EventCollectionConverter(), events);

                Validate(bson, "﻿wwAAAAMwALsAAAACJHR5cGUAkQAAAFNwYXJrLkluZnJhc3RydWN0dXJlLlNlcmlhbGl6YXRpb24uVGVzdHMuQ29udmVydGVycy5Vc2luZ0V2ZW50Q29sbGVjdGlvbkNvbnZlcnRlcitGYWtlRXZlbnQsIFNwYXJrLkluZnJhc3RydWN0dXJlLlNlcmlhbGl6YXRpb24uTmV3dG9uc29mdC5UZXN0cwACUHJvcGVydHkADAAAAE15IFByb3BlcnR5AAAA");
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var bson = default(Byte[]);
                var events = new EventCollection(new[] { new FakeEvent("My Property") });
                var document = new Dictionary<String, EventCollection> { { "events", events } };

                using (var memoryStream = new MemoryStream())
                {
                    NewtonsoftBsonSerializer.Default.Serialize(memoryStream, document);

                    bson = memoryStream.ToArray();
                }

                using (var memoryStream = new MemoryStream(bson, writable: false))
                {
                    document = NewtonsoftBsonSerializer.Default.Deserialize<Dictionary<String, EventCollection>>(memoryStream);
                }

                Assert.Equal("My Property", document["events"].OfType<FakeEvent>().Single().Property);
            }
        }

        public class FakeEvent : Event
        {
            public String Property { get; private set; }

            public FakeEvent(String property)
            {
                Property = property;
            }
        }
    }
}
