using System;
using Spark.Cqrs.Commanding;
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
    namespace UsingCommandEnvelopeConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(default(CommandEnvelope));

                Validate("null", json);
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var aggregateId = Guid.Parse("a6c45a28-c572-4d5b-ac18-7b0ec2d723fb");
                var envelope = new CommandEnvelope(aggregateId, new FakeCommand("My Command"));
                var json = WriteJson(envelope);

                Validate(json, @"
{
  ""a"": ""a6c45a28-c572-4d5b-ac18-7b0ec2d723fb"",
  ""c"": {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingCommandEnvelopeConverter.FakeCommand, Spark.Serialization.Newtonsoft.Tests"",
    ""Property"": ""My Command""
  }
}");
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<Message<CommandEnvelope>>("null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var envelope = ReadJson<CommandEnvelope>(@"
{
  ""a"": ""a6c45a28-c572-4d5b-ac18-7b0ec2d723fb"",
  ""c"": {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingCommandEnvelopeConverter.FakeCommand, Spark.Serialization.Newtonsoft.Tests"",
    ""Property"": ""My Command""
  }
}");
                Assert.Equal(Guid.Parse("a6c45a28-c572-4d5b-ac18-7b0ec2d723fb"), envelope.AggregateId);
            }

            [Fact]
            public void PropertyOrderIrrelevant()
            {
                var envelope = ReadJson<CommandEnvelope>(@"
{
  ""c"": {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingCommandEnvelopeConverter.FakeCommand, Spark.Serialization.Newtonsoft.Tests"",
    ""Property"": ""My Command""
  },
  ""a"": ""a6c45a28-c572-4d5b-ac18-7b0ec2d723fb""
}");
                Assert.Equal(Guid.Parse("a6c45a28-c572-4d5b-ac18-7b0ec2d723fb"), envelope.AggregateId);
            }

            [Fact]
            public void CanTolerateMalformedJson()
            {
                var envelope = ReadJson<CommandEnvelope>(@"
{
  ""a"": ""a6c45a28-c572-4d5b-ac18-7b0ec2d723fb"",
  ""c"": {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingCommandEnvelopeConverter.FakeCommand, Spark.Serialization.Newtonsoft.Tests"",
    ""Property"": ""My Command""
  },
}");
                Assert.Equal(Guid.Parse("a6c45a28-c572-4d5b-ac18-7b0ec2d723fb"), envelope.AggregateId);
            }
        }

        public class WhenWritingBson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeToBson()
            {
                var aggregateId = Guid.Parse("61296B2F-F040-472D-95DF-B1C3A32A7C7E");
                var envelope = new CommandEnvelope(aggregateId, new FakeCommand("My Command"));
                var bson = WriteBson(envelope);

                Validate(bson, "vQAAAAVhABAAAAAEL2spYUDwLUeV37HDoyp8fgNjAJ0AAAACJHR5cGUAdAAAAFRlc3QuU3BhcmsuU2VyaWFsaXphdGlvbi5Db252ZXJ0ZXJzLlVzaW5nQ29tbWFuZEVudmVsb3BlQ29udmVydGVyLkZha2VDb21tYW5kLCBTcGFyay5TZXJpYWxpemF0aW9uLk5ld3RvbnNvZnQuVGVzdHMAAlByb3BlcnR5AAsAAABNeSBDb21tYW5kAAAA");
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var bson = "vQAAAAVhABAAAAAEL2spYUDwLUeV37HDoyp8fgNjAJ0AAAACJHR5cGUAdAAAAFRlc3QuU3BhcmsuU2VyaWFsaXphdGlvbi5Db252ZXJ0ZXJzLlVzaW5nQ29tbWFuZEVudmVsb3BlQ29udmVydGVyLkZha2VDb21tYW5kLCBTcGFyay5TZXJpYWxpemF0aW9uLk5ld3RvbnNvZnQuVGVzdHMAAlByb3BlcnR5AAsAAABNeSBDb21tYW5kAAAA";
                var envelope = ReadBson<CommandEnvelope>(bson);

                Assert.Equal(Guid.Parse("61296B2F-F040-472D-95DF-B1C3A32A7C7E"), envelope.AggregateId);
            }
        }

        internal class FakeCommand : Command
        {
            public String Property { get; private set; }

            public FakeCommand(String property)
            {
                Property = property;
            }
        }
    }
}
