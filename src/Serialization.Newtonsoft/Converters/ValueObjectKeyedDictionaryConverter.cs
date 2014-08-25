using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

/* Copyright (c) 2014 Spark Software Ltd.
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
    /// Converts a dictionary keyed by a <see cref="ValueObject"/> from JSON.
    /// </summary>
    public sealed class ValueObjectKeyedDictionaryConverter : JsonConverter
    {
        private static readonly IDictionary<Type, Type[]> TypeCache = new ConcurrentDictionary<Type, Type[]>();
        private static readonly Type ValueObjectType = typeof(ValueObject);

        /// <summary>
        /// Gets a value indicating whether this <see cref="JsonConverter"/> can write JSON.
        /// </summary>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        public override Boolean CanConvert(Type objectType)
        {
            var dictionaryType = objectType.GetTypeHierarchy().SingleOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>));
            return dictionaryType != null && ValueObjectType.IsAssignableFrom(dictionaryType.GetGenericArguments().First());
        }

        /// <summary>
        /// Writes the JSON representation of a <see cref="ValueObject"/> instance.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Reads the JSON representation of a <see cref="ValueObject"/> instance.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">The type of object.</param>
        /// <param name="existingValue">The existing value of the object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, JsonSerializer serializer)
        {
            if (!reader.CanReadObject())
                return null;

            var genericTypes = GetGenericTypeArguments(objectType);
            var dictionary = (IDictionary)Activator.CreateInstance(objectType);
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                String propertyName;
                if (!reader.TryGetProperty(out propertyName))
                    continue;

                dictionary.Add(ValueObject.Parse(genericTypes.Key, propertyName), serializer.Deserialize(reader, genericTypes.Value));
            }

            return dictionary;
        }

        /// <summary>
        /// Get the generic type arguments for the underlying dictionary type.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        private KeyValuePair<Type, Type> GetGenericTypeArguments(Type objectType)
        {
            Type[] result;

            if (!TypeCache.TryGetValue(objectType, out result))
                TypeCache[objectType] = result = objectType.GetTypeHierarchy().Single(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)).GetGenericArguments();

            return new KeyValuePair<Type, Type>(result[0], result[1]);
        }
    }
}
