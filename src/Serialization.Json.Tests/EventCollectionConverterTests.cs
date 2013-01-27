using System;
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
    public static class UsingEventCollectionConverter
    {
        public class WhenCheckingCanConvert
        {
            [Fact]
            public void ReturnTrueIfTypeIsEventCollection()
            {
                var converter = new EventCollectionConverter();

                Assert.True(converter.CanConvert(typeof(EventCollection)));
            }

            [Fact]
            public void ReturnFalseIfTypeIsNotEventCollection()
            {
                var converter = new EventCollectionConverter();

                Assert.False(converter.CanConvert(typeof(HeaderCollection)));
            }
        }

        public class WhenSerializingObjects
        {
            [Fact]
            public void WriteEmptyArrayIfNoEvents()
            {
                var collection = new EventCollection(new Object[0]);
                var converter = new EventCollectionConverter();
                var json = new StringBuilder();

                using (var stringWriter = new StringWriter(json))
                using (var jsonWriter = new JsonTextWriter(stringWriter))
                    converter.WriteJson(jsonWriter, collection, new NewtonsoftJsonSerializer());

                Assert.Equal("[]", json.ToString());
            }

            [Fact]
            public void WriteArrayWithEventsIfNotEmpty()
            {
                var collection = new EventCollection(new Object[] { 1, 2, 3 });
                var converter = new EventCollectionConverter();
                var json = new StringBuilder();

                using (var stringWriter = new StringWriter(json))
                using (var jsonWriter = new JsonTextWriter(stringWriter))
                    converter.WriteJson(jsonWriter, collection, new NewtonsoftJsonSerializer());

                Assert.Equal("[1,2,3]", json.ToString());
            }
        }

        public class WhenDeserializingObjects
        {
            [Fact]
            public void ReadEmptyCollectionIfEmptyJsonArray()
            {
                var collection = default(EventCollection);
                var converter = new EventCollectionConverter();

                using (var stringReader = new StringReader("[]"))
                using (var jsonReader = new JsonTextReader(stringReader))
                    collection = (EventCollection)converter.ReadJson(jsonReader, typeof(EventCollection), null, new NewtonsoftJsonSerializer());

                Assert.Equal(0, collection.Count);
            }

            [Fact]
            public void ReadPopulatedCollectionIfNotEmptyJsonArray()
            {
                var collection = default(EventCollection);
                var converter = new EventCollectionConverter();

                using (var stringReader = new StringReader("[1,2,3]"))
                using (var jsonReader = new JsonTextReader(stringReader))
                    collection = (EventCollection)converter.ReadJson(jsonReader, typeof(EventCollection), null, new NewtonsoftJsonSerializer());

                Assert.Equal(3, collection.Count);
            }
        }
    }
}
