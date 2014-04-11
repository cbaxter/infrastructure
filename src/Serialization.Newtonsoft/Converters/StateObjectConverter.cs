﻿using System;
using System.Collections.Generic;
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

        /// <summary>
        /// The default <see cref="StateObjectConverter"/> instance.
        /// </summary>
        public static readonly StateObjectConverter Default = new StateObjectConverter();

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        public override Boolean CanConvert(Type objectType)
        {
            return StateObjectType.IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Writes the JSON representation of a <see cref="StateObject"/> instance.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
        {
            WriteJson(writer, null, (StateObject)value, serializer);
        }

        /// <summary>
        /// Writes the JSON representation of a <see cref="StateObject"/> instance.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="objectType">The object type.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="serializer">The calling serializer.</param>
        public void WriteJson(JsonWriter writer, Type objectType, StateObject value, JsonSerializer serializer)
        {
            var state = value.GetState();
            var instanceType = value.GetType();

            writer.WriteStartObject();

            // Determine if we must write out the state object type (i.e., $type property) or if we can simply infer the type from the objectType provided.
            if (objectType != instanceType)
            {
                writer.WritePropertyName(TypePropertyName);
                serializer.Serialize(writer, instanceType.GetFullNameWithAssembly());
            }

            // Write out each state object property value.
            foreach (var item in state)
            {
                writer.WritePropertyName(item.Key);

                if (item.Value == null)
                {
                    writer.WriteNull();
                }
                else
                {
                    WriteProperty(writer, value.GetFieldType(item.Key), item.Value, serializer);
                }
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes the JSON representation of a <see cref="StateObject"/> property value.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="propertyType">The property type.</param>
        /// <param name="propertyValue">The property value to serialize.</param>
        /// <param name="serializer">The calling serializer.</param>
        private void WriteProperty(JsonWriter writer, Type propertyType, Object propertyValue, JsonSerializer serializer)
        {
            var stateObject = propertyValue as StateObject;
            if (stateObject == null)
            {
                serializer.Serialize(writer, propertyValue, propertyType);
            }
            else
            {
                WriteJson(writer, propertyType, stateObject, serializer);
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
            if (!reader.CanReadObject())
                return null;

            var stateObject = default(StateObject);
            var state = new Dictionary<String, Object>();
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                var propertyName = String.Empty;
                if (!reader.TryGetProperty(out propertyName))
                    continue;

                if (propertyName == TypePropertyName)
                {
                    objectType = Type.GetType(serializer.Deserialize<String>(reader), throwOnError: true, ignoreCase: true);
                    stateObject = (StateObject)Activator.CreateInstance(objectType);
                }
                else
                {
                    stateObject = stateObject ?? (StateObject)Activator.CreateInstance(objectType);
                    state.Add(propertyName, serializer.Deserialize(reader, stateObject.GetFieldType(propertyName)));
                }
            }

            if (stateObject != null)
                stateObject.SetState(state);

            return stateObject;
        }
    }
}
