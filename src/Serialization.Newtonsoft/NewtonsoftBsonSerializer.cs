using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

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

namespace Spark.Infrastructure.Serialization
{    
    /// <summary>
    /// A BSON serializer using on JSON.NET.
    /// </summary>
    public sealed class NewtonsoftBsonSerializer : ISerializeObjects
    {
        public static readonly NewtonsoftBsonSerializer Default = new NewtonsoftBsonSerializer(Enumerable.Empty<JsonConverter>());
        private readonly JsonSerializer serializer;

        /// <summary>
        /// The <see cref="JsonSerializer"/> used by this <see cref="NewtonsoftBsonSerializer"/> instance.
        /// </summary>
        public JsonSerializer Serializer { get { return serializer; } }

        /// <summary>
        /// Initializes a new instance of <see cref="NewtonsoftBsonSerializer"/> with a set of custom <see cref="JsonConverter"/> instances to be used by <see cref="ConverterContractResolver"/>.
        /// </summary>
        /// <param name="jsonConverters">The set of custom <see cref="JsonConverter"/> instances to be used by <see cref="ConverterContractResolver"/>.</param>
        public NewtonsoftBsonSerializer(IEnumerable<JsonConverter> jsonConverters)
        {
            serializer = new JsonSerializer
                {
                    ContractResolver = new ConverterContractResolver(jsonConverters),
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.All
                };
        }

        /// <summary>
        /// Serializes the object graph to the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> on which to serialize the object <paramref name="graph"/>.</param>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="type">The <see cref="Type"/> of object being serialized.</param>
        public void Serialize(Stream stream, Object graph, Type type)
        {
            using (var bsonWriter = new BsonWriter(stream))
                serializer.Serialize(bsonWriter, graph);
        }
        
        /// <summary>
        /// Deserialize an object graph from the speciied <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> from which to deserialize an object graph.</param>
        /// <param name="type">The <see cref="Type"/> of object being deserialized.</param>
        public Object Deserialize(Stream stream, Type type)
        {
            using (var bsonWriter = new BsonReader(stream))
                return serializer.Deserialize(bsonWriter);
        }
    }
}
