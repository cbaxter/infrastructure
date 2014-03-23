using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

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

namespace Spark.Serialization.Converters
{
    /// <summary>
    /// Converts a <see cref="StateObject"/> to and from JSON.
    /// </summary>
    public sealed class StateObjectConverter : JsonConverter
    {
        private const String TypePropertyName = "$type";
        private static readonly Type StateObjectType = typeof(StateObject);
        private static readonly IDictionary<Type, String> TypeShortAssemblyQualifiedName = new ConcurrentDictionary<Type, String>();

        // NOTE: Serialization Guide: http://james.newtonking.com/projects/json/help/index.html?topic=html/SerializationGuide.htm
        private static readonly HashSet<Type> ImplicitTypes = new HashSet<Type>
            {
                //String
                typeof(String),

                // Integer
                typeof(Byte),
                typeof(SByte), 
                typeof(UInt16), 
                typeof(Int16), 
                typeof(UInt32), 
                typeof(Int32), 
                typeof(UInt64),
                typeof(Int64), 

                // Float
                typeof(Single),
                typeof(Double), 
                typeof(Decimal), 

                // Other
                typeof(Byte[]), 
                typeof(DateTime), 
                typeof(Guid), 
                typeof(Type),
                typeof(TypeConverter)
            };

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        public override Boolean CanConvert(Type objectType)
        {
            return StateObjectType.IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Writes the JSON representation of an <see cref="StateObject"/> instance.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
        {
            var entity = value as StateObject;

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
                    serializer.Serialize(writer, item.Value, entity.GetFieldType(item.Key));
                }

                writer.WriteEndObject();
            }
        }

        /// <summary>
        /// Reads the JSON representation of an <see cref="StateObject"/> instance.
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

            var propertyName = String.Empty;
            while (reader.Read() && reader.TokenType != JsonToken.EndObject && !reader.TryGetProperty(out propertyName))
                continue; //NOTE: Given that JSON.NET expects `$type` to be the first property, we can make the same assumption to keep life simple.

            var type = Type.GetType(serializer.Deserialize<String>(reader), throwOnError: true, ignoreCase: true);
            var stateObject = (StateObject)Activator.CreateInstance(type);
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                if (!reader.TryGetProperty(out propertyName))
                    continue;

                var fieldType = stateObject.GetFieldType(propertyName);
                state.Add(propertyName, serializer.Deserialize(reader, fieldType.IsEnum || ImplicitTypes.Contains(fieldType) ? fieldType : typeof(Object)));
            }

            return stateObject;
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
