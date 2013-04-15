using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

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

namespace Spark.Infrastructure.Serialization.Json
{
    /// <summary>
    /// Serializes or Deserializes an object graph to or from the provided <see cref="Stream"/> using a <see cref="Newtonsoft.Json.JsonSerializer"/>.
    /// </summary>
    public class JsonSerializer : ISerializeObjects
    {
        private static readonly IReadOnlyList<JsonConverter> KnownJsonConverters;
        private readonly Newtonsoft.Json.JsonSerializer untypedJsonSerializer;
        private readonly Newtonsoft.Json.JsonSerializer typedJsonSerializer;
        private readonly HashSet<Type> knownTypes;

        //TODO: Untested and Incomplete implementation...

        static JsonSerializer()
        {
            KnownJsonConverters = typeof(JsonSerializer).Assembly
                                                        .GetTypes()
                                                        .Where(type => !type.IsAbstract && type.IsClass && type.DerivesFrom(typeof(JsonConverter)))
                                                        .Select(type => (JsonConverter)Activator.CreateInstance(type))
                                                        .AsReadOnly();
        }

        public JsonSerializer()
        {
            knownTypes = new HashSet<Type>(GetKnownTypes());
            untypedJsonSerializer = GetJsonSerializer();
            typedJsonSerializer = GetJsonSerializer();

            typedJsonSerializer.TypeNameHandling = TypeNameHandling.All;
        }

        protected virtual IEnumerable<Type> GetKnownTypes()
        {
            return new[] { typeof(List<Object>), typeof(Dictionary<String, Object>) };
        }

        protected virtual Newtonsoft.Json.JsonSerializer GetJsonSerializer()
        {
            var serializer = new Newtonsoft.Json.JsonSerializer
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };

            foreach (var jsonConverter in KnownJsonConverters)
                serializer.Converters.Add(jsonConverter);

            return serializer;
        }

        public void Serialize(Stream stream, Object graph)
        {
            var serializer = graph == null || knownTypes.Contains(graph.GetType()) ? untypedJsonSerializer : typedJsonSerializer;

            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
                serializer.Serialize(jsonWriter, graph);
        }

        public Object Deserialize(Stream stream)
        {
            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
                return typedJsonSerializer.Deserialize(jsonReader);
        }
    }
}
