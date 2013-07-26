using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Spark.Domain;
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

namespace Spark.Serialization.Tests.Converters
{
    public static class UsingStateObjectConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(new StateObjectConverter(), default(Entity));

                Validate("null", json);
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var entity = new TestAggregate();
                var json = WriteJson(new StateObjectConverter(), entity);

                Validate(
                    "{\"$type\":\"Spark.Serialization.Tests.Converters.UsingStateObjectConverter+TestAggregate, Spark.Serialization.Newtonsoft.Tests\",\"c\":{\"$type\":\"Spark.Domain.EntityCollection`1[[Spark.Domain.Entity, Spark.Core]], Spark.Core\",\"$values\":[{\"$type\":\"Spark.Serialization.Tests.Converters.UsingStateObjectConverter+TestEntity, Spark.Serialization.Newtonsoft.Tests\",\"id\":\"8cb5f171-5505-4313-b8a8-0345d70cfb46\",\"n\":\"My Entity\"}]},\"d\":8.9,\"f\":456.7,\"i\":123,\"n\":\"My Aggregate\",\"s\":1,\"t\":\"2013-07-01T00:00:00\"}",
                    json
                );
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<Entity>(new StateObjectConverter(), "null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var json = "{\"$type\":\"Spark.Serialization.Tests.Converters.UsingStateObjectConverter+TestAggregate, Spark.Serialization.Newtonsoft.Tests\",\"c\":{\"$type\":\"Spark.Domain.EntityCollection`1[[Spark.Domain.Entity, Spark.Core]], Spark.Core\",\"$values\":[{\"$type\":\"Spark.Serialization.Tests.Converters.UsingStateObjectConverter+TestEntity, Spark.Serialization.Newtonsoft.Tests\",\"id\":\"8cb5f171-5505-4313-b8a8-0345d70cfb46\",\"n\":\"My Entity\"}]},\"d\":8.9,\"f\":456.7,\"i\":123,\"n\":\"My Aggregate\",\"s\":1,\"t\":\"2013-07-01T00:00:00\"}";
                var entity = (TestAggregate)ReadJson<Entity>(new StateObjectConverter(), json);

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
                var bson = WriteBson(new StateObjectConverter(), entity);

                Validate(
                    "5wEAAAIkdHlwZQBzAAAAU3BhcmsuU2VyaWFsaXphdGlvbi5UZXN0cy5Db252ZXJ0ZXJzLlVzaW5nU3RhdGVPYmplY3RDb252ZXJ0ZXIrVGVzdEFnZ3JlZ2F0ZSwgU3BhcmsuU2VyaWFsaXphdGlvbi5OZXd0b25zb2Z0LlRlc3RzAANjABoBAAACJHR5cGUATwAAAFNwYXJrLkRvbWFpbi5FbnRpdHlDb2xsZWN0aW9uYDFbW1NwYXJrLkRvbWFpbi5FbnRpdHksIFNwYXJrLkNvcmVdXSwgU3BhcmsuQ29yZQAEJHZhbHVlcwCyAAAAAzAAqgAAAAIkdHlwZQBwAAAAU3BhcmsuU2VyaWFsaXphdGlvbi5UZXN0cy5Db252ZXJ0ZXJzLlVzaW5nU3RhdGVPYmplY3RDb252ZXJ0ZXIrVGVzdEVudGl0eSwgU3BhcmsuU2VyaWFsaXphdGlvbi5OZXd0b25zb2Z0LlRlc3RzAAVpZAAQAAAABHHxtYwFVRNDuKgDRdcM+0YCbgAKAAAATXkgRW50aXR5AAAAAAFkAM3MzMzMzCFAAWYAMzMzMzOLfEASaQB7AAAAAAAAAAJuAA0AAABNeSBBZ2dyZWdhdGUAEHMAAQAAAAl0AAD70Jg/AQAAAA==", 
                    bson
                );
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var bson = "5wEAAAIkdHlwZQBzAAAAU3BhcmsuU2VyaWFsaXphdGlvbi5UZXN0cy5Db252ZXJ0ZXJzLlVzaW5nU3RhdGVPYmplY3RDb252ZXJ0ZXIrVGVzdEFnZ3JlZ2F0ZSwgU3BhcmsuU2VyaWFsaXphdGlvbi5OZXd0b25zb2Z0LlRlc3RzAANjABoBAAACJHR5cGUATwAAAFNwYXJrLkRvbWFpbi5FbnRpdHlDb2xsZWN0aW9uYDFbW1NwYXJrLkRvbWFpbi5FbnRpdHksIFNwYXJrLkNvcmVdXSwgU3BhcmsuQ29yZQAEJHZhbHVlcwCyAAAAAzAAqgAAAAIkdHlwZQBwAAAAU3BhcmsuU2VyaWFsaXphdGlvbi5UZXN0cy5Db252ZXJ0ZXJzLlVzaW5nU3RhdGVPYmplY3RDb252ZXJ0ZXIrVGVzdEVudGl0eSwgU3BhcmsuU2VyaWFsaXphdGlvbi5OZXd0b25zb2Z0LlRlc3RzAAVpZAAQAAAABHHxtYwFVRNDuKgDRdcM+0YCbgAKAAAATXkgRW50aXR5AAAAAAFkAM3MzMzMzCFAAWYAMzMzMzOLfEASaQB7AAAAAAAAAAJuAA0AAABNeSBBZ2dyZWdhdGUAEHMAAQAAAAl0AAD70Jg/AQAAAA==";
                var entity = (TestAggregate)ReadBson<Entity>(new StateObjectConverter(), bson);
                
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
