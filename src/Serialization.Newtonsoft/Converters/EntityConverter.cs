using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Spark.Infrastructure.Domain;

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

namespace Spark.Infrastructure.Serialization.Converters
{
    /// <summary>
    /// Converts a <see cref="Entity"/> to and from JSON.
    /// </summary>
    public sealed class EntityConverter : JsonConverter
    {
        private const String TypePropertyName = "$type";
        private static readonly Type EntityType = typeof(Entity);
        private static readonly IDictionary<Type, String> TypeShortAssemblyQualifiedName = new ConcurrentDictionary<Type, String>();

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        public override Boolean CanConvert(Type objectType)
        {
            return objectType == EntityType;
        }

        /// <summary>
        /// Writes the JSON representation of an <see cref="Entity"/> instance.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
        {
            var entity = value as Entity;

            if (entity == null)
            {
                writer.WriteNull();
            }
            else
            {
                var state = entity.GetState();
                var assemblyName = GetShortAssemblyQualifiedName(value.GetType());

                writer.WriteStartObject();
                writer.WritePropertyName(TypePropertyName);
                serializer.Serialize(writer, assemblyName);

                foreach (var item in state)
                {
                    writer.WritePropertyName(item.Key);
                    serializer.Serialize(writer, item.Value);
                }

                writer.WriteEndObject();
            }
        }

        /// <summary>
        /// Reads the JSON representation of an <see cref="Entity"/> instance.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">The type of object.</param>
        /// <param name="existingValue">The existing value of the object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, JsonSerializer serializer)
        {
            var state = new Dictionary<String, Object>();

            if (!reader.CanReadObject())
                return null;

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                String propertyName;
                if (!reader.TryGetProperty(out propertyName))
                    continue;

                state.Add(propertyName, serializer.Deserialize<Object>(reader));
            }

            return CreateEntity(objectType, state);
        }

        /// <summary>
        /// Craete a new instance of the target <see cref="Entity"/> type and specified <paramref name="state"/> data.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        /// <param name="state">The parsed JSON state.</param>
        private static Entity CreateEntity(Type objectType, IDictionary<String, Object> state)
        {
            var entityType = GetEntityType(objectType, state);
            var entity = (Entity)Activator.CreateInstance(entityType);

            entity.SetState(state);

            return entity;
        }

        /// <summary>
        /// Get the target entity type from the stored state or use the requested <paramref name="objectType"/> if not found.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        /// <param name="state">The parsed JSON state.</param>
        private static Type GetEntityType(Type objectType, IDictionary<String, Object> state)
        {
            Object value;

            return state.TryGetValue(TypePropertyName, out value) && value != null ? Type.GetType(value.ToString(), throwOnError: true, ignoreCase: true) : objectType;
        }

        /// <summary>
        /// Get the short assembly qualified type name (i.e., Namespace.Type, Assembly)
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which the short assembly qualified name is to be retrieved.</param>
        private static String GetShortAssemblyQualifiedName(Type type)
        {
            String result;

            if (!TypeShortAssemblyQualifiedName.TryGetValue(type, out result))
                TypeShortAssemblyQualifiedName[type] = result = String.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name);

            return result;
        }
    }
}
