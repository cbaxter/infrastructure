using System;
using Newtonsoft.Json;
using Spark.Cqrs.Eventing;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Spark.Serialization.Converters
{
    /// <summary>
    /// Converts a <see cref="EventEnvelope"/> to and from JSON.
    /// </summary>
    public sealed class EventEnvelopeConverter : JsonConverter
    {
        private static readonly Type EventEnvelopeType = typeof(EventEnvelope);

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        public override Boolean CanConvert(Type objectType)
        {
            return objectType == EventEnvelopeType;
        }

        /// <summary>
        /// Writes the JSON representation of an <see cref="EventEnvelope"/> instance.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
        {
            var envelope = (EventEnvelope)value;

            writer.WriteStartObject();

            writer.WritePropertyName("a");
            writer.WriteValue(envelope.AggregateId);

            writer.WritePropertyName("v");
            serializer.Serialize(writer, envelope.Version, typeof(EventVersion));

            writer.WritePropertyName("e");
            serializer.Serialize(writer, envelope.Event, typeof(Event));

            writer.WritePropertyName("c");
            writer.WriteValue(envelope.CorrelationId);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads the JSON representation of an <see cref="EventEnvelope"/> instance.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">The type of object.</param>
        /// <param name="existingValue">The existing value of the object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, JsonSerializer serializer)
        {
            if (!reader.CanReadObject())
                return null;

            var e = default(Event);
            var aggregateId = Guid.Empty;
            var correlationId = Guid.Empty;
            var version = EventVersion.Empty;
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
                    case "v":
                        version = serializer.Deserialize<EventVersion>(reader);
                        break;
                    case "e":
                        e = serializer.Deserialize<Event>(reader);
                        break;
                    case "c":
                        correlationId = serializer.Deserialize<Guid>(reader);
                        break;
                }
            }

            return new EventEnvelope(correlationId, aggregateId, version, e);
        }
    }
}
