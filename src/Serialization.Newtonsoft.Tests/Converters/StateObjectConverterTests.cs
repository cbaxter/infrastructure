using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Spark.Cqrs.Domain;
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
    public static class UsingStateObjectConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(default(Entity));

                Validate("null", json);
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var entity = new TestAggregate();
                var json = WriteJson(entity);

                Validate2(json, @"
{
  ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestAggregate, Spark.Serialization.Newtonsoft.Tests"",
  ""c"": [
    {
      ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestEntity, Spark.Serialization.Newtonsoft.Tests"",
      ""id"": ""8cb5f171-5505-4313-b8a8-0345d70cfb46"",
      ""n"": ""My Entity""
    }
  ],
  ""d"": 8.9,
  ""f"": 456.7,
  ""i"": 123,
  ""n"": ""My Aggregate"",
  ""s"": 1,
  ""t"": ""2013-07-01T00:00:00""
}");
            }

            [Fact]
            public void WriteOutStateObjectPropertyTypeIfNotInstanceType()
            {
                var entity = new TestAggregateWithEntityParent(new TestEntity());
                var json = WriteJson(entity);

                Validate2(json, @"
{
  ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestAggregateWithEntityParent, Spark.Serialization.Newtonsoft.Tests"",
  ""p"": {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestEntity, Spark.Serialization.Newtonsoft.Tests"",
    ""id"": ""8cb5f171-5505-4313-b8a8-0345d70cfb46"",
    ""n"": ""My Entity""
  }
}");
            }

            [Fact]
            public void DoNotWriteOutStateObjectPropertyTypeIfInstanceType()
            {
                var entity = new TestAggregateWithTestEntityParent(new TestEntity());
                var json = WriteJson(entity);

                Validate2(json, @"
{
  ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestAggregateWithTestEntityParent, Spark.Serialization.Newtonsoft.Tests"",
  ""p"": {
    ""id"": ""8cb5f171-5505-4313-b8a8-0345d70cfb46"",
    ""n"": ""My Entity""
  }
}");
            }

            [Fact]
            public void Unnamed1()
            {
                var entity = new TestAggregateWithEntityChildren();
                var json = WriteJson(entity);

                Validate2(json, @"
{
  ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestAggregateWithEntityChildren, Spark.Serialization.Newtonsoft.Tests"",
  ""c"": [
    {
      ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestEntity, Spark.Serialization.Newtonsoft.Tests"",
      ""id"": ""8cb5f171-5505-4313-b8a8-0345d70cfb46"",
      ""n"": ""My Entity""
    },
    {
      ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+DerrivedEntity, Spark.Serialization.Newtonsoft.Tests"",
      ""a"": null,
      ""id"": ""3bdf361d-f577-4e01-801e-375b949fd14a"",
      ""n"": ""My Entity""
    }
  ]
}");
            }

            [Fact]
            public void Unnamed2()
            {
                var entity = new TestAggregateWithTestEntityChildren();
                var json = WriteJson(entity);

                Validate2(json, @"
{
  ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestAggregateWithTestEntityChildren, Spark.Serialization.Newtonsoft.Tests"",
  ""c"": [
    {
      ""id"": ""8cb5f171-5505-4313-b8a8-0345d70cfb46"",
      ""n"": ""My Entity""
    },
    {
      ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+DerrivedEntity, Spark.Serialization.Newtonsoft.Tests"",
      ""a"": null,
      ""id"": ""3bdf361d-f577-4e01-801e-375b949fd14a"",
      ""n"": ""My Entity""
    }
  ]
}");
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<Entity>("null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var entity = (TestAggregate)ReadJson<Entity>(@"
{
  ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestAggregate, Spark.Serialization.Newtonsoft.Tests"",
  ""c"": [
    {
      ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestEntity, Spark.Serialization.Newtonsoft.Tests"",
      ""id"": ""8cb5f171-5505-4313-b8a8-0345d70cfb46"",
      ""n"": ""My Entity""
    }
  ],
  ""d"": 8.9,
  ""f"": 456.7,
  ""i"": 123,
  ""n"": ""My Aggregate"",
  ""s"": 1,
  ""t"": ""2013-07-01T00:00:00""
}");

                Assert.Equal("My Entity", entity.Children.Cast<TestEntity>().Single().Name);
                Assert.Equal("My Aggregate", entity.Name);
                Assert.Equal(123, entity.Number);
                Assert.Equal(456.7, entity.Double);
                Assert.Equal(8.9M, entity.Decimal);
                Assert.Equal(TestEnum.Serialized, entity.Status);
                Assert.Equal(DateTime.Parse("2013-07-01"), entity.Timestamp);
            }

            [Fact]
            public void ReadStateObjectPropertyTypeIfNotInstanceType()
            {
                var entity = ReadJson<TestAggregateWithEntityParent>(@"
{
  ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestAggregateWithEntityParent, Spark.Serialization.Newtonsoft.Tests"",
  ""p"": {
    ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestEntity, Spark.Serialization.Newtonsoft.Tests"",
    ""id"": ""8cb5f171-5505-4313-b8a8-0345d70cfb46"",
    ""n"": ""My Entity""
  }
}");

                Assert.IsType(typeof(TestEntity), entity.Parent);
            }

            [Fact]
            public void InferStateObjectPropertyTypeIfInstanceType()
            {
                var entity = ReadJson<TestAggregateWithTestEntityParent>(@"
{
  ""$type"": ""Test.Spark.Serialization.Converters.UsingStateObjectConverter+TestAggregateWithTestEntityParent, Spark.Serialization.Newtonsoft.Tests"",
  ""p"": {
    ""id"": ""8cb5f171-5505-4313-b8a8-0345d70cfb46"",
    ""n"": ""My Entity""
  }
}");

                Assert.IsType(typeof(TestEntity), entity.Parent);
            }
        }

        public class WhenWritingBson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeToBson()
            {
                var entity = new TestAggregate();
                var bson = WriteBson(entity);

                Validate(
                    "fQEAAAIkdHlwZQByAAAAVGVzdC5TcGFyay5TZXJpYWxpemF0aW9uLkNvbnZlcnRlcnMuVXNpbmdTdGF0ZU9iamVjdENvbnZlcnRlcitUZXN0QWdncmVnYXRlLCBTcGFyay5TZXJpYWxpemF0aW9uLk5ld3RvbnNvZnQuVGVzdHMABGMAsQAAAAMwAKkAAAACJHR5cGUAbwAAAFRlc3QuU3BhcmsuU2VyaWFsaXphdGlvbi5Db252ZXJ0ZXJzLlVzaW5nU3RhdGVPYmplY3RDb252ZXJ0ZXIrVGVzdEVudGl0eSwgU3BhcmsuU2VyaWFsaXphdGlvbi5OZXd0b25zb2Z0LlRlc3RzAAVpZAAQAAAABHHxtYwFVRNDuKgDRdcM+0YCbgAKAAAATXkgRW50aXR5AAAAAWQAzczMzMzMIUABZgAzMzMzM4t8QBJpAHsAAAAAAAAAAm4ADQAAAE15IEFnZ3JlZ2F0ZQAQcwABAAAACXQAAPvQmD8BAAAA",
                    bson
                );
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var bson = "fQEAAAIkdHlwZQByAAAAVGVzdC5TcGFyay5TZXJpYWxpemF0aW9uLkNvbnZlcnRlcnMuVXNpbmdTdGF0ZU9iamVjdENvbnZlcnRlcitUZXN0QWdncmVnYXRlLCBTcGFyay5TZXJpYWxpemF0aW9uLk5ld3RvbnNvZnQuVGVzdHMABGMAsQAAAAMwAKkAAAACJHR5cGUAbwAAAFRlc3QuU3BhcmsuU2VyaWFsaXphdGlvbi5Db252ZXJ0ZXJzLlVzaW5nU3RhdGVPYmplY3RDb252ZXJ0ZXIrVGVzdEVudGl0eSwgU3BhcmsuU2VyaWFsaXphdGlvbi5OZXd0b25zb2Z0LlRlc3RzAAVpZAAQAAAABHHxtYwFVRNDuKgDRdcM+0YCbgAKAAAATXkgRW50aXR5AAAAAWQAzczMzMzMIUABZgAzMzMzM4t8QBJpAHsAAAAAAAAAAm4ADQAAAE15IEFnZ3JlZ2F0ZQAQcwABAAAACXQAAPvQmD8BAAAA";
                var entity = (TestAggregate)ReadBson<Entity>(bson);

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
            public EntityCollection<Entity> Children { get; set; }

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

        public sealed class TestAggregateWithEntityParent : Aggregate
        {
            [DataMember(Name = "p")]
            public Entity Parent { get; set; }

            public TestAggregateWithEntityParent()
            { }

            public TestAggregateWithEntityParent(Entity parent)
            {
                Id = Guid.Parse("8D5A1320-8B4E-4890-BA4E-02A8CF5D4F81");
                Parent = parent;
            }
        }

        public sealed class TestAggregateWithTestEntityParent : Aggregate
        {
            [DataMember(Name = "p")]
            public TestEntity Parent { get; set; }

            public TestAggregateWithTestEntityParent()
            { }

            public TestAggregateWithTestEntityParent(TestEntity parent)
            {
                Id = Guid.Parse("8D5A1320-8B4E-4890-BA4E-02A8CF5D4F81");
                Parent = parent;
            }
        }

        public sealed class TestAggregateWithEntityChildren : Aggregate
        {
            [DataMember(Name = "c")]
            public EntityCollection<Entity> Children { get; set; }

            public TestAggregateWithEntityChildren()
            {
                Id = Guid.Parse("8D5A1320-8B4E-4890-BA4E-02A8CF5D4F81");
                Children = new EntityCollection<Entity> { new TestEntity(), new DerrivedEntity() };
            }
        }

        public sealed class TestAggregateWithTestEntityChildren : Aggregate
        {
            [DataMember(Name = "c")]
            private EntityCollection<TestEntity> Children { get; set; }

            public TestAggregateWithTestEntityChildren()
            {
                Id = Guid.Parse("8D5A1320-8B4E-4890-BA4E-02A8CF5D4F81");
                Children = new EntityCollection<TestEntity> { new TestEntity(), new DerrivedEntity() };
            }
        }

        public class TestEntity : Entity
        {
            [DataMember(Name = "n")]
            public String Name { get; set; }

            public TestEntity()
            {
                Id = Guid.Parse("8CB5F171-5505-4313-B8A8-0345D70CFB46");
                Name = "My Entity";
            }
        }

        public class DerrivedEntity : TestEntity
        {
            [DataMember(Name = "a")]
            public String AnotherName { get; set; }

            public DerrivedEntity()
            {
                Id = Guid.Parse("3BDF361D-F577-4E01-801E-375B949FD14A");
                Name = "My Entity";
            }
        }
    }
}
