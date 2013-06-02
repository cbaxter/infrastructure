using System;
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
    public static class UsingMessageConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(new MessageConverter(), default(Message<CommandEnvelope>));

                Validate("null", json);
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var id = Guid.NewGuid();
                var aggregateId = Guid.NewGuid();
                var message = new Message<CommandEnvelope>(id, HeaderCollection.Empty, new CommandEnvelope(aggregateId, new FakeCommand("My Command")));
                var json = WriteJson(new MessageConverter(), message);

                Validate(
                    String.Format("﻿{{\"id\":\"{0}\",\"h\":{{}},\"p\":{{\"a\":\"{1}\",\"c\":{{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingMessageConverter+FakeCommand, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"Property\":\"My Command\"}}}}}}", id, aggregateId), 
                    json
                );
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<Message<CommandEnvelope>>(new MessageConverter(), "null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var json = "﻿{\"id\":\"f3487646-bd4d-4bc4-8fb2-1f7f2e2a232d\",\"h\":{},\"p\":{\"a\":\"a6c45a28-c572-4d5b-ac18-7b0ec2d723fb\",\"c\":{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingMessageConverter+FakeCommand, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"Property\":\"My Command\"}}}";
                var message = ReadJson<Message<CommandEnvelope>>(new MessageConverter(), json);

                Assert.Equal(Guid.Parse("f3487646-bd4d-4bc4-8fb2-1f7f2e2a232d"), message.Id);
            }

            [Fact]
            public void PropertyOrderIrrelevant()
            {
                var json = "﻿{\"p\":{\"a\":\"a6c45a28-c572-4d5b-ac18-7b0ec2d723fb\",\"c\":{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingMessageConverter+FakeCommand, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"Property\":\"My Command\"}},\"h\":{},\"id\":\"f3487646-bd4d-4bc4-8fb2-1f7f2e2a232d\"}";
                var message = ReadJson<Message<CommandEnvelope>>(new MessageConverter(), json);

                Assert.Equal(Guid.Parse("f3487646-bd4d-4bc4-8fb2-1f7f2e2a232d"), message.Id);
            }

            [Fact]
            public void CanTolerateMalformedJson()
            {
                var json = "﻿{\"id\":\"f3487646-bd4d-4bc4-8fb2-1f7f2e2a232d\",\"h\":";
                var message = ReadJson<Message<CommandEnvelope>>(new MessageConverter(), json);

                Assert.Equal(Guid.Parse("f3487646-bd4d-4bc4-8fb2-1f7f2e2a232d"), message.Id);
            }
        }

        public class WhenWritingBson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeToBson()
            {
                var id = Guid.Parse("A96662A6-ABA8-4CF9-9882-86E7F52BF18C");
                var aggregateId = Guid.Parse("61296B2F-F040-472D-95DF-B1C3A32A7C7E");
                var message = new Message<CommandEnvelope>(id, HeaderCollection.Empty, new CommandEnvelope(aggregateId, new FakeCommand("My Command")));
                var json = WriteBson(new MessageConverter(), message);

                Validate(json, "﻿/QAAAAVpZAAQAAAABKZiZqmoq/lMmIKG5/Ur8YwDaAAFAAAAAANwANQAAAAFYQAQAAAABC9rKWFA8C1Hld+xw6MqfH4DYwC0AAAAAiR0eXBlAIsAAABTcGFyay5JbmZyYXN0cnVjdHVyZS5TZXJpYWxpemF0aW9uLlRlc3RzLkNvbnZlcnRlcnMuVXNpbmdNZXNzYWdlQ29udmVydGVyK0Zha2VDb21tYW5kLCBTcGFyay5JbmZyYXN0cnVjdHVyZS5TZXJpYWxpemF0aW9uLk5ld3RvbnNvZnQuVGVzdHMAAlByb3BlcnR5AAsAAABNeSBDb21tYW5kAAAAAA==");
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var bson = "﻿/QAAAAVpZAAQAAAABKZiZqmoq/lMmIKG5/Ur8YwDaAAFAAAAAANwANQAAAAFYQAQAAAABC9rKWFA8C1Hld+xw6MqfH4DYwC0AAAAAiR0eXBlAIsAAABTcGFyay5JbmZyYXN0cnVjdHVyZS5TZXJpYWxpemF0aW9uLlRlc3RzLkNvbnZlcnRlcnMuVXNpbmdNZXNzYWdlQ29udmVydGVyK0Zha2VDb21tYW5kLCBTcGFyay5JbmZyYXN0cnVjdHVyZS5TZXJpYWxpemF0aW9uLk5ld3RvbnNvZnQuVGVzdHMAAlByb3BlcnR5AAsAAABNeSBDb21tYW5kAAAAAA==";
                var message = ReadBson<Message<CommandEnvelope>>(new MessageConverter(), bson);

                Assert.Equal(Guid.Parse("A96662A6-ABA8-4CF9-9882-86E7F52BF18C"), message.Id);
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
