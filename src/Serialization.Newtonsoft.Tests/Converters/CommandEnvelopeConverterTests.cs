﻿using System;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Messaging;
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
    public static class UsingCommandEnvelopeConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(new CommandEnvelopeConverter(), default(CommandEnvelope));

                Validate("null", json);
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var aggregateId = Guid.NewGuid();
                var envelope = new CommandEnvelope(aggregateId, new FakeCommand("My Command"));
                var json = WriteJson(new CommandEnvelopeConverter(), envelope);

                Validate(
                    String.Format("{{\"a\":\"{0}\",\"c\":{{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingCommandEnvelopeConverter+FakeCommand, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"Property\":\"My Command\"}}}}", aggregateId), 
                    json
                );
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<Message<CommandEnvelope>>(new CommandEnvelopeConverter(), "null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var json = "﻿{\"a\":\"a6c45a28-c572-4d5b-ac18-7b0ec2d723fb\",\"c\":{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingMessageConverter+FakeCommand, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"Property\":\"My Command\"}}";
                var envelope = ReadJson<CommandEnvelope>(new CommandEnvelopeConverter(), json);

                Assert.Equal(Guid.Parse("a6c45a28-c572-4d5b-ac18-7b0ec2d723fb"), envelope.AggregateId);
            }

            [Fact]
            public void PropertyOrderIrrelevant()
            {
                var json = "﻿{\"c\":{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingMessageConverter+FakeCommand, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"Property\":\"My Command\"},\"a\":\"a6c45a28-c572-4d5b-ac18-7b0ec2d723fb\"}";
                var envelope = ReadJson<CommandEnvelope>(new CommandEnvelopeConverter(), json);

                Assert.Equal(Guid.Parse("a6c45a28-c572-4d5b-ac18-7b0ec2d723fb"), envelope.AggregateId);
            }

            [Fact]
            public void CanTolerateMalformedJson()
            {
                var json = "﻿{\"a\":\"a6c45a28-c572-4d5b-ac18-7b0ec2d723fb\",\"c\":{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingMessageConverter+FakeCommand, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"Property\":\"My Command\"},\"x\":null,}";
                var envelope = ReadJson<CommandEnvelope>(new CommandEnvelopeConverter(), json);

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
                var json = WriteBson(new CommandEnvelopeConverter(), envelope);

                Validate(json, "﻿3AAAAAVhABAAAAAEL2spYUDwLUeV37HDoyp8fgNjALwAAAACJHR5cGUAkwAAAFNwYXJrLkluZnJhc3RydWN0dXJlLlNlcmlhbGl6YXRpb24uVGVzdHMuQ29udmVydGVycy5Vc2luZ0NvbW1hbmRFbnZlbG9wZUNvbnZlcnRlcitGYWtlQ29tbWFuZCwgU3BhcmsuSW5mcmFzdHJ1Y3R1cmUuU2VyaWFsaXphdGlvbi5OZXd0b25zb2Z0LlRlc3RzAAJQcm9wZXJ0eQALAAAATXkgQ29tbWFuZAAAAA==");
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var bson = "3AAAAAVhABAAAAAEL2spYUDwLUeV37HDoyp8fgNjALwAAAACJHR5cGUAkwAAAFNwYXJrLkluZnJhc3RydWN0dXJlLlNlcmlhbGl6YXRpb24uVGVzdHMuQ29udmVydGVycy5Vc2luZ0NvbW1hbmRFbnZlbG9wZUNvbnZlcnRlcitGYWtlQ29tbWFuZCwgU3BhcmsuSW5mcmFzdHJ1Y3R1cmUuU2VyaWFsaXphdGlvbi5OZXd0b25zb2Z0LlRlc3RzAAJQcm9wZXJ0eQALAAAATXkgQ29tbWFuZAAAAA==";
                var envelope = ReadBson<CommandEnvelope>(new CommandEnvelopeConverter(), bson);

                Assert.Equal(Guid.Parse("61296B2F-F040-472D-95DF-B1C3A32A7C7E"), envelope.AggregateId);
            }
        }

        public class FakeCommand : Command
        {
            public String Property { get; private set; }

            public FakeCommand(String property)
            {
                Property = property;
            }
        }
    }
}