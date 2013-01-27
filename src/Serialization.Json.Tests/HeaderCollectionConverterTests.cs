using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Serialization.Json;
using Xunit;
using NewtonsoftJsonSerializer = Newtonsoft.Json.JsonSerializer;

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

namespace Serialization.Json.Tests
{
    public static class UsingHeaderCollectionConverter
    {
        public class WhenCheckingCanConvert
        {
            [Fact]
            public void ReturnTrueIfTypeIsHeaderCollection()
            {
                var converter = new HeaderCollectionConverter();

                Assert.True(converter.CanConvert(typeof(HeaderCollection)));
            }

            [Fact]
            public void ReturnFalseIfTypeIsNotHeaderCollection()
            {
                var converter = new HeaderCollectionConverter();

                Assert.False(converter.CanConvert(typeof(EventCollection)));
            }
        }

        public class WhenSerializingObjects
        {
            [Fact]
            public void WriteEmptyDictionaryIfNoHeaders()
            {
                var collection = new HeaderCollection(new Dictionary<String, Object>(0));
                var converter = new HeaderCollectionConverter();
                var json = new StringBuilder();

                using (var stringWriter = new StringWriter(json))
                using (var jsonWriter = new JsonTextWriter(stringWriter))
                    converter.WriteJson(jsonWriter, collection, new NewtonsoftJsonSerializer());

                Assert.Equal("{}", json.ToString());
            }

            [Fact]
            public void WriteDictionaryWithValuesIfNotEmpty()
            {
                var collection = new HeaderCollection(new Dictionary<String, Object> { { "Name", "Value" } });
                var converter = new HeaderCollectionConverter();
                var json = new StringBuilder();

                using (var stringWriter = new StringWriter(json))
                using (var jsonWriter = new JsonTextWriter(stringWriter))
                    converter.WriteJson(jsonWriter, collection, new NewtonsoftJsonSerializer());

                Assert.Equal("{\"Name\":\"Value\"}", json.ToString());
            }
        }

        public class WhenDeserializingObjects
        {
            [Fact]
            public void ReadEmptyCollectionIfEmptyJsonObject()
            {
                var collection = default(HeaderCollection);
                var converter = new HeaderCollectionConverter();

                using (var stringReader = new StringReader("{}"))
                using (var jsonReader = new JsonTextReader(stringReader))
                    collection = (HeaderCollection)converter.ReadJson(jsonReader, typeof(HeaderCollection), null, new NewtonsoftJsonSerializer());

                Assert.Equal(0, collection.Count);
            }

            [Fact]
            public void ReadPopulatedCollectionIfNotEmptyJsonObjecty()
            {
                var collection = default(HeaderCollection);
                var converter = new HeaderCollectionConverter();

                using (var stringReader = new StringReader("{\"Name1\":\"Value1\",\"Name2\":\"Value2\",\"Name3\":\"Value3\"}"))
                using (var jsonReader = new JsonTextReader(stringReader))
                    collection = (HeaderCollection)converter.ReadJson(jsonReader, typeof(HeaderCollection), null, new NewtonsoftJsonSerializer());

                Assert.Equal(3, collection.Count);
            }
        }
    }
}
