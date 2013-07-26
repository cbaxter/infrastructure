using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
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
    public abstract class UsingJsonConverter
    {
        protected String WriteBson(JsonConverter converter, Object value)
        {
            using (var memoryStream = new MemoryStream())
            using (var bsonWriter = new BsonWriter(memoryStream))
            {
                converter.WriteJson(bsonWriter, value, NewtonsoftBsonSerializer.Default.Serializer);

                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        protected String WriteJson(JsonConverter converter, Object value)
        {
            var sb = new StringBuilder();

            using (var stringWriter = new StringWriter(sb))
            using (var jsonWriter = new JsonTextWriter(stringWriter))
                converter.WriteJson(jsonWriter, value, NewtonsoftJsonSerializer.Default.Serializer);

            return sb.ToString();
        }

        protected T ReadBson<T>(JsonConverter converter, String bson)
        {
            using (var memoryStream = new MemoryStream(Convert.FromBase64String(RemovePreamble(bson)), writable: false))
            using (var bsonReader = new BsonReader(memoryStream))
                return (T)converter.ReadJson(bsonReader, typeof(T), null, NewtonsoftBsonSerializer.Default.Serializer);
        }

        protected T ReadJson<T>(JsonConverter converter, String json)
        {
            using (var stringReader = new StringReader(RemovePreamble(json)))
            using (var jsonReader = new JsonTextReader(stringReader))
                return (T)converter.ReadJson(jsonReader, typeof(T), null, NewtonsoftJsonSerializer.Default.Serializer);
        }

        protected void Validate(String expected, String actual)
        {
            Assert.Equal(RemovePreamble(expected), RemovePreamble(actual));
        }

        private String RemovePreamble(String value)
        {
            return value.Length > 0 && value[0] == 65279 ? value.Substring(1) : value;
        }
    }
}
