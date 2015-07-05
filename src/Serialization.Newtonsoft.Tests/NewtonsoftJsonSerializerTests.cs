using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
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

namespace Test.Spark.Serialization
{
    namespace UsingNewtonsoftJsonSerializer
    {
        public class WhenSerializingData
        {
            [Fact]
            public void IncludeTypeWhenDeclaringTypeDoesNotMatchSpecifiedType()
            {
                String json;
                using (var memoryStream = new MemoryStream())
                {
                    NewtonsoftJsonSerializer.Default.Serializer.Formatting = Formatting.None;
                    NewtonsoftJsonSerializer.Default.Serialize(memoryStream, new Dictionary<String, Object> { { "key", "value" } }, typeof(Object));

                    json = Encoding.UTF8.GetString(memoryStream.ToArray());
                }

                Assert.Contains("{\"$type\":\"System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Object, mscorlib]], mscorlib\",\"key\":\"value\"}", json);
            }
        }

        public class WhenDeserializingData
        {
            [Fact]
            public void UseIncludedTypeWhenSpecified()
            {
                Byte[] json = Encoding.UTF8.GetBytes("{\"$type\":\"System.Collections.Generic.Dictionary`2[[System.String, mscorlib],[System.Object, mscorlib]], mscorlib\",\"key\":\"value\"}");
                IDictionary<String, Object> graph;

                using (var memoryStream = new MemoryStream(json, writable: false))
                {
                    NewtonsoftJsonSerializer.Default.Serializer.Formatting = Formatting.None;

                    graph = (IDictionary<String, Object>)NewtonsoftJsonSerializer.Default.Deserialize(memoryStream, typeof(Object));
                }

                Assert.Equal("value", graph["key"]);
            }

            [Fact]
            public void UseDefaultConstructorWhenSpecified()
            {
                Byte[] json = Encoding.UTF8.GetBytes("{\"$type\":\"Test.Spark.Serialization.UsingNewtonsoftJsonSerializer.WhenDeserializingData+ClassWithDefaultConstructor, Spark.Serialization.Newtonsoft.Tests\",\"test\":\"Test String\"}");
                ClassWithDefaultConstructor graph;

                using (var memoryStream = new MemoryStream(json, writable: false))
                {
                    NewtonsoftJsonSerializer.Default.Serializer.Formatting = Formatting.None;

                    graph = (ClassWithDefaultConstructor)NewtonsoftJsonSerializer.Default.Deserialize(memoryStream, typeof(Object));
                }

                Assert.Equal("Test String", graph.Test);
                Assert.True(graph.DefaultConstructorInvoked);
            }
            
            [Fact]
            public void CanUseCustomDataMemberNames()
            {
                Byte[] json = Encoding.UTF8.GetBytes("{\"$type\":\"Test.Spark.Serialization.UsingNewtonsoftJsonSerializer.WhenDeserializingData+ClassWithCustomMemberName, Spark.Serialization.Newtonsoft.Tests\",\"t\":\"Test String\"}");
                ClassWithCustomMemberName graph;

                using (var memoryStream = new MemoryStream(json, writable: false))
                {
                    NewtonsoftJsonSerializer.Default.Serializer.Formatting = Formatting.None;

                    graph = (ClassWithCustomMemberName)NewtonsoftJsonSerializer.Default.Deserialize(memoryStream, typeof(Object));
                }

                Assert.Equal("Test String", graph.Test);
            }

            [Fact]
            public void CanUsePrivateSetterInFullTrustEnvironment()
            {
                Byte[] json = Encoding.UTF8.GetBytes("{\"$type\":\"Test.Spark.Serialization.UsingNewtonsoftJsonSerializer.WhenDeserializingData+ClassWithPrivateSetter, Spark.Serialization.Newtonsoft.Tests\",\"t\":\"Test String\"}");
                ClassWithPrivateSetter graph;

                using (var memoryStream = new MemoryStream(json, writable: false))
                {
                    NewtonsoftJsonSerializer.Default.Serializer.Formatting = Formatting.None;

                    graph = (ClassWithPrivateSetter)NewtonsoftJsonSerializer.Default.Deserialize(memoryStream, typeof(Object));
                }

                Assert.Equal("Test String", graph.Test);
            }

            public class ClassWithDefaultConstructor
            {
                public String Test { get; private set; }
                public Boolean DefaultConstructorInvoked { get; private set; }

                public ClassWithDefaultConstructor()
                {
                    Test = String.Empty;
                    DefaultConstructorInvoked = true;
                }
            }

            public class ClassWithCustomConstructor
            {
                public String Test { get; private set; }
                public Boolean CustomConstructorInvoked { get; private set; }

                public ClassWithCustomConstructor(String test)
                {
                    Test = test;
                    CustomConstructorInvoked = true;
                }
            }

            public class ClassWithCustomMemberName
            {
                [DataMember(Name = "t")]
                public String Test { get; set; }

                public ClassWithCustomMemberName(String test)
                {
                    Test = test;
                }
            }

            public class ClassWithPrivateSetter
            {
                [DataMember(Name = "t")]
                public String Test { get; private set; }

                public ClassWithPrivateSetter(String test)
                {
                    Test = test;
                }
            }
        }
    }
}
