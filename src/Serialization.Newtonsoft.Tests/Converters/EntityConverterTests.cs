using System;
using System.Collections.Generic;
using System.Linq;
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
                    "{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingEntityConverter+TestAggregate, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"FakeList\":{\"$type\":\"System.Collections.Generic.List`1[[System.Int32, mscorlib]], mscorlib\",\"$values\":[1,2,3]}}",
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
                var json = "{\"$type\":\"Spark.Infrastructure.Serialization.Tests.Converters.UsingEntityConverter+TestAggregate, Spark.Infrastructure.Serialization.Newtonsoft.Tests\",\"FakeList\":{\"$type\":\"System.Collections.Generic.List`1[[System.Int32, mscorlib]], mscorlib\",\"$values\":[1,2,3]}}";
                var entity = (TestAggregate)ReadJson<Entity>(new EntityConverter(), json);

                Assert.Equal(3, entity.FakeList.Count());
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
                    "HwEAAAIkdHlwZQCMAAAAU3BhcmsuSW5mcmFzdHJ1Y3R1cmUuU2VyaWFsaXphdGlvbi5UZXN0cy5Db252ZXJ0ZXJzLlVzaW5nRW50aXR5Q29udmVydGVyK1Rlc3RBZ2dyZWdhdGUsIFNwYXJrLkluZnJhc3RydWN0dXJlLlNlcmlhbGl6YXRpb24uTmV3dG9uc29mdC5UZXN0cwADRmFrZUxpc3QAeQAAAAIkdHlwZQBGAAAAU3lzdGVtLkNvbGxlY3Rpb25zLkdlbmVyaWMuTGlzdGAxW1tTeXN0ZW0uSW50MzIsIG1zY29ybGliXV0sIG1zY29ybGliAAQkdmFsdWVzABoAAAAQMAABAAAAEDEAAgAAABAyAAMAAAAAAAA=", 
                    bson
                );
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var bson = "﻿HwEAAAIkdHlwZQCMAAAAU3BhcmsuSW5mcmFzdHJ1Y3R1cmUuU2VyaWFsaXphdGlvbi5UZXN0cy5Db252ZXJ0ZXJzLlVzaW5nRW50aXR5Q29udmVydGVyK1Rlc3RBZ2dyZWdhdGUsIFNwYXJrLkluZnJhc3RydWN0dXJlLlNlcmlhbGl6YXRpb24uTmV3dG9uc29mdC5UZXN0cwADRmFrZUxpc3QAeQAAAAIkdHlwZQBGAAAAU3lzdGVtLkNvbGxlY3Rpb25zLkdlbmVyaWMuTGlzdGAxW1tTeXN0ZW0uSW50MzIsIG1zY29ybGliXV0sIG1zY29ybGliAAQkdmFsdWVzABoAAAAQMAABAAAAEDEAAgAAABAyAAMAAAAAAAA=";
                var entity = (TestAggregate)ReadBson<Entity>(new EntityConverter(), bson);

                Assert.Equal(3, entity.FakeList.Count());
            }
        }

        public sealed class TestAggregate : Aggregate
        {
            public IEnumerable<Int32> FakeList { get; set; }

            public TestAggregate()
            {
                Id = Guid.Parse("8D5A1320-8B4E-4890-BA4E-02A8CF5D4F81");
                FakeList = new List<Int32> { 1, 2, 3 };
            }
        }
    }
}
