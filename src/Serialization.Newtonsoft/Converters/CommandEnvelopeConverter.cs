using System;
using Newtonsoft.Json;
using Spark.Cqrs.Commanding;

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
    /// Converts a <see cref="CommandEnvelope"/> to and from JSON.
    /// </summary>
    public sealed class CommandEnvelopeConverter : JsonConverter
    {
        private static readonly Type CommandEnvelopeType = typeof(CommandEnvelope);

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        public override Boolean CanConvert(Type objectType)
        {
            return objectType == CommandEnvelopeType;
        }

        /// <summary>
        /// Writes the JSON representation of a <see cref="CommandEnvelope"/> instance.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
        {
            var envelope = (CommandEnvelope)value;

            writer.WriteStartObject();

            writer.WritePropertyName("a");
            writer.WriteValue(envelope.AggregateId);

            writer.WritePropertyName("c");
            serializer.Serialize(writer, envelope.Command, typeof(Command));

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads the JSON representation of a <see cref="CommandEnvelope"/> instance.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">The type of object.</param>
        /// <param name="existingValue">The existing value of the object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, JsonSerializer serializer)
        {
            if (!reader.CanReadObject())
                return null;

            var aggregateId = Guid.Empty;
            var command = default(Command);
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                String propertyName;
                if (!reader.TryGetProperty(out propertyName))
                    continue;

                switch (propertyName)
                {
                    case "a":
                        aggregateId = serializer.Deserialize<Guid>(reader);
                        break;
                    case "c":
                        command = serializer.Deserialize<Command>(reader);
                        break;
                }
            }

            return new CommandEnvelope(aggregateId, command);
        }
    }
}
