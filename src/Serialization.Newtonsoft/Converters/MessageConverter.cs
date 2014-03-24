using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Spark.Cqrs.Eventing;
using Spark.Messaging;

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
    /// Converts a <see cref="EventCollection"/> to and from JSON.
    /// </summary>
    internal sealed class MessageConverter : JsonConverter
    {
        private static readonly Type MessageType = typeof(Message<>);
        private static readonly IDictionary<Type, Type> KnownPayloadTypes = new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        public override Boolean CanConvert(Type objectType)
        {
            Type payloadType;
            if (KnownPayloadTypes.TryGetValue(objectType, out payloadType))
                return true;

            if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == MessageType)
                KnownPayloadTypes[objectType] = payloadType = objectType.GetGenericArguments().Single();

            return payloadType != null;
        }

        /// <summary>
        /// Writes the JSON representation of an <see cref="EventCollection"/> instance.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
        {
            var message = (Message)value;

            writer.WriteStartObject();

            writer.WritePropertyName("id");
            writer.WriteValue(message.Id);

            writer.WritePropertyName("h");
            serializer.Serialize(writer, message.Headers);

            writer.WritePropertyName("p");
            serializer.Serialize(writer, message.GetPayload(), message.PayloadType);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads the JSON representation of an <see cref="EventCollection"/> instance.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">The type of object.</param>
        /// <param name="existingValue">The existing value of the object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, JsonSerializer serializer)
        {
            if (!reader.CanReadObject())
                return null;

            var id = Guid.Empty;
            var payload = default(Object);
            var headers = HeaderCollection.Empty;
            var payloadType = KnownPayloadTypes[objectType];
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                String propertyName;
                if (!reader.TryGetProperty(out propertyName))
                    continue;

                switch (propertyName)
                {
                    case "id":
                        id = serializer.Deserialize<Guid>(reader);
                        break;
                    case "h":
                        headers = serializer.Deserialize<HeaderCollection>(reader);
                        break;
                    case "p":
                        payload = serializer.Deserialize(reader, payloadType);
                        break;
                }
            }

            return Activator.CreateInstance(objectType, id, headers, payload);
        }
    }
}
