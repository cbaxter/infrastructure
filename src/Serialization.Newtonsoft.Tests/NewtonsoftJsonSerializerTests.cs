using System;
using System.Collections.Generic;
using System.IO;
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
    public static class UsingNewtonsoftJsonSerializer
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

                    graph = (IDictionary<String, Object>) NewtonsoftJsonSerializer.Default.Deserialize(memoryStream, typeof (Object));
                }

                Assert.Equal("value", graph["key"]);
            }
        }
    }
}
