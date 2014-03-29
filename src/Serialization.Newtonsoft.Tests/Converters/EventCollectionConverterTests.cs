using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spark.Cqrs.Eventing;
using Spark.Serialization;
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
    public static class UsingEventCollectionConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(default(EventCollection));

                Validate("null", json);
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var events = new EventCollection(new[] { new FakeEvent("My Property") });
                var json = WriteJson(events);

                Validate(json, @"
[
  {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingEventCollectionConverter+FakeEvent, Spark.Serialization.Newtonsoft.Tests"",
    ""Property"": ""My Property""
  }
]");
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<EventCollection>("null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var events = ReadJson<EventCollection>(@"
[
  {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingEventCollectionConverter+FakeEvent, Spark.Serialization.Newtonsoft.Tests"",
    ""Property"": ""My Property""
  }
]");
                Assert.Equal("My Property", events.OfType<FakeEvent>().Single().Property);
            }
        }

        public class WhenWritingBson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeToBson()
            {
                var events = new EventCollection(new[] { new FakeEvent("My Property") });
                var bson = WriteBson(events);

                Validate("pAAAAAMwAJwAAAACJHR5cGUAcgAAAFRlc3QuU3BhcmsuU2VyaWFsaXphdGlvbi5Db252ZXJ0ZXJzLlVzaW5nRXZlbnRDb2xsZWN0aW9uQ29udmVydGVyK0Zha2VFdmVudCwgU3BhcmsuU2VyaWFsaXphdGlvbi5OZXd0b25zb2Z0LlRlc3RzAAJQcm9wZXJ0eQAMAAAATXkgUHJvcGVydHkAAAA=", bson);
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
