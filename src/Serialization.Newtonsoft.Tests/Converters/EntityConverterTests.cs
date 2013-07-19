using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Spark.Infrastructure.Domain;
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
    public static class UsingEntityConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(new EntityConverter(), default(Entity));

                Validate("null", json);
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var entity = new TestAggregate();
                var json = WriteJson(new EntityConverter(), entity);

                Validate(
                    "{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingEntityConverter+TestAggregate, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"c\":{\"$type\":\"Spark.Infrastructure.Domain.EntityCollection`1[[Spark.Infrastructure.Domain.Entity, Spark.Infrastructure.Core]], Spark.Infrastructure.Core\",\"$values\":[{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingEntityConverter+TestEntity, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"id\":\"8cb5f171-5505-4313-b8a8-0345d70cfb46\",\"n\":\"My Entity\"}]},\"d\":8.9,\"f\":456.7,\"i\":123,\"n\":\"My Aggregate\",\"s\":1,\"t\":\"2013-07-01T00:00:00\"}",
                    json
                );
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<Entity>(new EntityConverter(), "null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var json = "{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingEntityConverter+TestAggregate, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"c\":{\"$type\":\"Spark.Infrastructure.Domain.EntityCollection`1[[Spark.Infrastructure.Domain.Entity, Spark.Infrastructure.Core]], Spark.Infrastructure.Core\",\"$values\":[{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingEntityConverter+TestEntity, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"id\":\"8cb5f171-5505-4313-b8a8-0345d70cfb46\",\"n\":\"My Entity\"}]},\"d\":8.9,\"f\":456.7,\"i\":123,\"n\":\"My Aggregate\",\"s\":1,\"t\":\"2013-07-01T00:00:00\"}";
                var entity = (TestAggregate)ReadJson<Entity>(new EntityConverter(), json);

                Assert.Equal("My Entity", entity.Children.Cast<TestEntity>().Single().Name);
                Assert.Equal("My Aggregate", entity.Name);
                Assert.Equal(123, entity.Number);
                Assert.Equal(456.7, entity.Double);
                Assert.Equal(8.9M, entity.Decimal);
                Assert.Equal(TestEnum.Serialized, entity.Status);
                Assert.Equal(DateTime.Parse("2013-07-01"), entity.Timestamp);
            }
        }

        public class WhenWritingBson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeToBson()
            {
                var entity = new TestAggregate();
                var bson = WriteBson(new EntityConverter(), entity);

                Validate(
                    "VQIAAAIkdHlwZQCMAAAAU3BhcmsuSW5mcmFzdHJ1Y3R1cmUuU2VyaWFsaXphdGlvbi5UZXN0cy5Db252ZXJ0ZXJzLlVzaW5nRW50aXR5Q29udmVydGVyK1Rlc3RBZ2dyZWdhdGUsIFNwYXJrLkluZnJhc3RydWN0dXJlLlNlcmlhbGl6YXRpb24uTmV3dG9uc29mdC5UZXN0cwADYwBvAQAAAiR0eXBlAIsAAABTcGFyay5JbmZyYXN0cnVjdHVyZS5Eb21haW4uRW50aXR5Q29sbGVjdGlvbmAxW1tTcGFyay5JbmZyYXN0cnVjdHVyZS5Eb21haW4uRW50aXR5LCBTcGFyay5JbmZyYXN0cnVjdHVyZS5Db3JlXV0sIFNwYXJrLkluZnJhc3RydWN0dXJlLkNvcmUABCR2YWx1ZXMAywAAAAMwAMMAAAACJHR5cGUAiQAAAFNwYXJrLkluZnJhc3RydWN0dXJlLlNlcmlhbGl6YXRpb24uVGVzdHMuQ29udmVydGVycy5Vc2luZ0VudGl0eUNvbnZlcnRlcitUZXN0RW50aXR5LCBTcGFyay5JbmZyYXN0cnVjdHVyZS5TZXJpYWxpemF0aW9uLk5ld3RvbnNvZnQuVGVzdHMABWlkABAAAAAEcfG1jAVVE0O4qANF1wz7RgJuAAoAAABNeSBFbnRpdHkAAAAAAWQAzczMzMzMIUABZgAzMzMzM4t8QBJpAHsAAAAAAAAAAm4ADQAAAE15IEFnZ3JlZ2F0ZQAQcwABAAAACXQAAPvQmD8BAAAA", 
                    bson
                );
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var bson = "﻿VQIAAAIkdHlwZQCMAAAAU3BhcmsuSW5mcmFzdHJ1Y3R1cmUuU2VyaWFsaXphdGlvbi5UZXN0cy5Db252ZXJ0ZXJzLlVzaW5nRW50aXR5Q29udmVydGVyK1Rlc3RBZ2dyZWdhdGUsIFNwYXJrLkluZnJhc3RydWN0dXJlLlNlcmlhbGl6YXRpb24uTmV3dG9uc29mdC5UZXN0cwADYwBvAQAAAiR0eXBlAIsAAABTcGFyay5JbmZyYXN0cnVjdHVyZS5Eb21haW4uRW50aXR5Q29sbGVjdGlvbmAxW1tTcGFyay5JbmZyYXN0cnVjdHVyZS5Eb21haW4uRW50aXR5LCBTcGFyay5JbmZyYXN0cnVjdHVyZS5Db3JlXV0sIFNwYXJrLkluZnJhc3RydWN0dXJlLkNvcmUABCR2YWx1ZXMAywAAAAMwAMMAAAACJHR5cGUAiQAAAFNwYXJrLkluZnJhc3RydWN0dXJlLlNlcmlhbGl6YXRpb24uVGVzdHMuQ29udmVydGVycy5Vc2luZ0VudGl0eUNvbnZlcnRlcitUZXN0RW50aXR5LCBTcGFyay5JbmZyYXN0cnVjdHVyZS5TZXJpYWxpemF0aW9uLk5ld3RvbnNvZnQuVGVzdHMABWlkABAAAAAEcfG1jAVVE0O4qANF1wz7RgJuAAoAAABNeSBFbnRpdHkAAAAAAWQAzczMzMzMIUABZgAzMzMzM4t8QBJpAHsAAAAAAAAAAm4ADQAAAE15IEFnZ3JlZ2F0ZQAQcwABAAAACXQAAPvQmD8BAAAA";
                var entity = (TestAggregate)ReadBson<Entity>(new EntityConverter(), bson);
                
                Assert.Equal("My Entity", entity.Children.Cast<TestEntity>().Single().Name);
                Assert.Equal("My Aggregate", entity.Name);
                Assert.Equal(123, entity.Number);
                Assert.Equal(456.7, entity.Double);
                Assert.Equal(8.9M, entity.Decimal);
                Assert.Equal(TestEnum.Serialized, entity.Status);
                Assert.Equal(DateTime.Parse("2013-07-01"), entity.Timestamp);
            }
        }

        public enum TestEnum
        {
            Unknown = 0,
            Serialized = 1
        }

        public sealed class TestAggregate : Aggregate
        {
            [DataMember(Name = "c")]
            public IEnumerable<Entity> Children { get; set; }

            [DataMember(Name = "n")]
            public String Name { get; set; }

            [DataMember(Name = "t")]
            public DateTime Timestamp { get; set; }

            [DataMember(Name = "i")]
            public Int64 Number { get; set; }

            [DataMember(Name = "f")]
            public Double Double { get; set; }

            [DataMember(Name = "d")]
            public Decimal Decimal { get; set; }

            [DataMember(Name = "s")]
            public TestEnum Status { get; set; }
 
            public TestAggregate()
            {
                Version = 10;
                Id = Guid.Parse("8D5A1320-8B4E-4890-BA4E-02A8CF5D4F81");
                Children = new EntityCollection<Entity> { new TestEntity() };
                Timestamp = DateTime.Parse("2013-07-01");
                Status = TestEnum.Serialized;
                Name = "My Aggregate";
                Decimal = 8.9M;
                Double = 456.7;
                Number = 123;
            }
        }

        public sealed class TestEntity : Entity
        {
            [DataMember(Name = "n")]
            public String Name { get; set; }

            public TestEntity()
            {
                Id = Guid.Parse("8CB5F171-5505-4313-B8A8-0345D70CFB46");
                Name = "My Entity";
            }
        }
    }
}
