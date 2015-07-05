using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Spark.Serialization;
using Xunit;

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

namespace Test.Spark.Serialization.Converters
{
    public abstract class UsingJsonConverter
    {
        protected String WriteBson(Object value)
        {
            using (var memoryStream = new MemoryStream())
            using (var bsonWriter = new BsonWriter(memoryStream))
            {
                NewtonsoftBsonSerializer.Default.Serializer.Serialize(bsonWriter, value);

                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        protected String WriteBson<T>(T value, JsonConverter converter)
        {
            if (!converter.CanConvert(typeof(T)))
                throw new InvalidOperationException();

            using (var memoryStream = new MemoryStream())
            using (var bsonWriter = new BsonWriter(memoryStream))
            {
                converter.WriteJson(bsonWriter, value, NewtonsoftBsonSerializer.Default.Serializer);

                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        protected String WriteJson(Object value)
        {
            var sb = new StringBuilder();

            NewtonsoftJsonSerializer.Default.Serializer.Formatting = Formatting.Indented;

            using (var stringWriter = new StringWriter(sb))
            using (var jsonWriter = new JsonTextWriter(stringWriter))
                NewtonsoftJsonSerializer.Default.Serializer.Serialize(jsonWriter, value);

            return sb.ToString();
        }

        protected String WriteJson<T>(T value, JsonConverter converter)
        {
            if (!converter.CanConvert(typeof(T)))
                throw new InvalidOperationException();

            NewtonsoftJsonSerializer.Default.Serializer.Formatting = Formatting.Indented;

            var sb = new StringBuilder();
            using (var stringWriter = new StringWriter(sb))
            using (var jsonWriter = new JsonTextWriter(stringWriter))
                converter.WriteJson(jsonWriter, value, NewtonsoftJsonSerializer.Default.Serializer);

            return sb.ToString();
        }

        protected T ReadBson<T>(String bson)
        {
            return NewtonsoftBsonSerializer.Default.Deserialize<T>(Convert.FromBase64String(RemovePreamble(bson)));
        }

        protected T ReadBson<T>(String bson, JsonConverter converter)
        {
            if (!converter.CanConvert(typeof(T)))
                throw new InvalidOperationException();

            using (var memoryStream = new MemoryStream(Convert.FromBase64String(RemovePreamble(bson)), writable: false))
            using (var bsonReader = new BsonReader(memoryStream))
                return (T)converter.ReadJson(bsonReader, typeof(T), null, NewtonsoftBsonSerializer.Default.Serializer);
        }

        protected T ReadJson<T>(String json)
        {
            return NewtonsoftJsonSerializer.Default.Deserialize<T>(Encoding.UTF8.GetBytes(json));
        }

        protected T ReadJson<T>(String json, JsonConverter converter)
        {
            if (!converter.CanConvert(typeof(T)))
                throw new InvalidOperationException();

            using (var stringReader = new StringReader(RemovePreamble(json)))
            using (var jsonReader = new JsonTextReader(stringReader))
                return (T)converter.ReadJson(jsonReader, typeof(T), null, NewtonsoftBsonSerializer.Default.Serializer);
        }

        protected void Validate(String actual, String expected)
        {
            actual =  RemovePreamble(actual).Trim().Replace("\r\n", "\n");
            expected = RemovePreamble(expected).Trim().Replace("\r\n", "\n");

            Assert.Equal(expected, actual);
        }

        private static String RemovePreamble(String value)
        {
            return value.Length > 0 && value[0] == 65279 ? value.Substring(1) : value;
        }
    }
}
