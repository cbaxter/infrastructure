using System;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Eventing;
using Spark.Messaging;
using Xunit;

/* Copyright (c) 2014 Spark Software Ltd.
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
    namespace UsingEventEnvelopeConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(default(EventEnvelope));

                Validate("null", json);
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var version = new EventVersion(3, 2, 1);
                var aggregateId = Guid.Parse("512fe943-c9bd-49c3-8116-20c186c755af");
                var correlationId = Guid.Parse("4fabd791-ef41-4d6b-9579-fcd9189d492b");
                var envelope = new EventEnvelope(correlationId, aggregateId, version, new FakeEvent("My Event"));
                var json = WriteJson(envelope);

                Validate(json, @"
{
  ""a"": ""512fe943-c9bd-49c3-8116-20c186c755af"",
  ""v"": {
    ""v"": 3,
    ""c"": 2,
    ""i"": 1
  },
  ""e"": {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingEventEnvelopeConverter.FakeEvent, Spark.Serialization.Newtonsoft.Tests"",
    ""Property"": ""My Event""
  },
  ""c"": ""4fabd791-ef41-4d6b-9579-fcd9189d492b""
}");
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<Message<EventEnvelope>>("null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var envelope = ReadJson<EventEnvelope>(@"
{
  ""a"": ""512fe943-c9bd-49c3-8116-20c186c755af"",
  ""v"": {
    ""v"": 3,
    ""c"": 2,
    ""i"": 1
  },
  ""e"": {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingEventEnvelopeConverter.FakeEvent, Spark.Serialization.Newtonsoft.Tests"",
    ""Property"": ""My Event""
  },
  ""c"": ""4fabd791-ef41-4d6b-9579-fcd9189d492b""
}");
                Assert.Equal(new EventVersion(3, 2, 1), envelope.Version);
                Assert.Equal(Guid.Parse("512fe943-c9bd-49c3-8116-20c186c755af"), envelope.AggregateId);
                Assert.Equal(Guid.Parse("4fabd791-ef41-4d6b-9579-fcd9189d492b"), envelope.CorrelationId);
            }

            [Fact]
            public void PropertyOrderIrrelevant()
            {
                var envelope = ReadJson<EventEnvelope>(@"
{
  ""a"": ""512fe943-c9bd-49c3-8116-20c186c755af"",
  ""c"": ""4fabd791-ef41-4d6b-9579-fcd9189d492b"",
  ""v"": {
    ""v"": 3,
    ""c"": 2,
    ""i"": 1
  },
  ""e"": {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingEventEnvelopeConverter.FakeEvent, Spark.Serialization.Newtonsoft.Tests"",
    ""Property"": ""My Event""
  }
}");
                Assert.Equal(new EventVersion(3, 2, 1), envelope.Version);
                Assert.Equal(Guid.Parse("512fe943-c9bd-49c3-8116-20c186c755af"), envelope.AggregateId);
                Assert.Equal(Guid.Parse("4fabd791-ef41-4d6b-9579-fcd9189d492b"), envelope.CorrelationId);
            }

            [Fact]
            public void CanTolerateMalformedJson()
            {
                var envelope = ReadJson<EventEnvelope>(@"
{
  ""a"": ""512fe943-c9bd-49c3-8116-20c186c755af"",
  ""v"": {
    ""v"": 3,
    ""c"": 2,
    ""i"": 1
  },
  ""e"": {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingEventEnvelopeConverter.FakeEvent, Spark.Serialization.Newtonsoft.Tests"",
    ""Property"": ""My Event""
  },
  ""c"": ""4fabd791-ef41-4d6b-9579-fcd9189d492b"",
}");
                Assert.Equal(Guid.Parse("512fe943-c9bd-49c3-8116-20c186c755af"), envelope.AggregateId);
            }
        }

        public class WhenWritingBson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeToBson()
            {
                var version = new EventVersion(3, 2, 1);
                var aggregateId = Guid.Parse("512fe943-c9bd-49c3-8116-20c186c755af");
                var correlationId = Guid.Parse("4fabd791-ef41-4d6b-9579-fcd9189d492b");
                var envelope = new EventEnvelope(correlationId, aggregateId, version, new FakeEvent("My Event"));
                var bson = WriteBson(envelope);

                Validate(bson, "7AAAAAVhABAAAAAEQ+kvUb3Jw0mBFiDBhsdVrwN2ABoAAAAQdgADAAAAEGMAAgAAABBpAAEAAAAAA2UAlwAAAAIkdHlwZQBwAAAAVGVzdC5TcGFyay5TZXJpYWxpemF0aW9uLkNvbnZlcnRlcnMuVXNpbmdFdmVudEVudmVsb3BlQ29udmVydGVyLkZha2VFdmVudCwgU3BhcmsuU2VyaWFsaXphdGlvbi5OZXd0b25zb2Z0LlRlc3RzAAJQcm9wZXJ0eQAJAAAATXkgRXZlbnQAAAVjABAAAAAEkderT0Hva02VefzZGJ1JKwA=");
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var bson = "7AAAAAVhABAAAAAEQ+kvUb3Jw0mBFiDBhsdVrwN2ABoAAAAQdgADAAAAEGMAAgAAABBpAAEAAAAAA2UAlwAAAAIkdHlwZQBwAAAAVGVzdC5TcGFyay5TZXJpYWxpemF0aW9uLkNvbnZlcnRlcnMuVXNpbmdFdmVudEVudmVsb3BlQ29udmVydGVyLkZha2VFdmVudCwgU3BhcmsuU2VyaWFsaXphdGlvbi5OZXd0b25zb2Z0LlRlc3RzAAJQcm9wZXJ0eQAJAAAATXkgRXZlbnQAAAVjABAAAAAEkderT0Hva02VefzZGJ1JKwA=";
                var envelope = ReadBson<EventEnvelope>(bson);
                
                Assert.Equal(new EventVersion(3, 2, 1), envelope.Version);
                Assert.Equal(Guid.Parse("512fe943-c9bd-49c3-8116-20c186c755af"), envelope.AggregateId);
                Assert.Equal(Guid.Parse("4fabd791-ef41-4d6b-9579-fcd9189d492b"), envelope.CorrelationId);
            }
        }

        internal class FakeEvent : Event
        {
            public String Property { get; private set; }

            public FakeEvent(String property)
            {
                Property = property;
            }
        }
    }
}
