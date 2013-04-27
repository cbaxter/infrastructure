﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Spark.Infrastructure.Messaging;

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
    /// Converts a <see cref="HeaderCollection"/> to and from JSON.
    /// </summary>
    public sealed class HeaderCollectionConverter : JsonConverter
    {
        private static readonly Type HeaderCollectionType = typeof(HeaderCollection);

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        public override Boolean CanConvert(Type objectType)
        {
            return objectType == HeaderCollectionType;
        }

        /// <summary>
        /// Writes the JSON representation of an <see cref="HeaderCollection"/> instance.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, Object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            var items = (HeaderCollection)value;

            writer.WriteStartObject();

            foreach (var item in items)
            {
                writer.WritePropertyName(item.Key);
                serializer.Serialize(writer, item.Value);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads the JSON representation of an <see cref="HeaderCollection"/> instance.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">The type of object.</param>
        /// <param name="existingValue">The existing value of the object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            var dictionary = new Dictionary<String, Object>();

            if (reader.TokenType == JsonToken.None)
                reader.Read();

            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                dictionary.Add((String)reader.Value, reader.Read() ? serializer.Deserialize(reader, reader.ValueType) : null);

            return new HeaderCollection(dictionary);
        }
    }
}