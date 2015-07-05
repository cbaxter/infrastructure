using System;
using Newtonsoft.Json;
using Spark.Cqrs.Domain;

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
    /// Converts a <see cref="EntityCollection{TEntity}"/> to and from JSON.
    /// </summary>
    public sealed class EntityCollectionConverter : JsonConverter
    {
        private static readonly Type EntityCollectionType = typeof(EntityCollection);

        /// <summary>
        /// The default <see cref="StateObjectConverter"/> instance.
        /// </summary>
        public static readonly EntityCollectionConverter Default = new EntityCollectionConverter();

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        public override Boolean CanConvert(Type objectType)
        {
            return EntityCollectionType.IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Writes the JSON representation of an <see cref="EntityCollection{T}"/> instance.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                var collection = (EntityCollection)value;

                writer.WriteStartArray();

                foreach (var entity in collection)
                    StateObjectConverter.Default.WriteJson(writer, collection.EntityType, entity, serializer);

                writer.WriteEndArray();
            }
        }

        /// <summary>
        /// Reads the JSON representation of an <see cref="EntityCollection{T}"/> instance.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">The type of object.</param>
        /// <param name="existingValue">The existing value of the object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, JsonSerializer serializer)
        {
            var collection = (EntityCollection)Activator.CreateInstance(objectType);

            if (reader.CanReadArray())
            {
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                    collection.Add((Entity)StateObjectConverter.Default.ReadJson(reader, collection.EntityType, null, serializer));
            }

            return collection;
        }
    }
}
