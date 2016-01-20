using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

namespace Spark.Serialization
{
    /// <summary>
    /// A JSON serializer using on JSON.NET.
    /// </summary>
    public sealed class NewtonsoftJsonSerializer : ISerializeObjects
    {
        /// <summary>
        /// The default Newtonsoft JSON Serializer instance.
        /// </summary>
        public static readonly NewtonsoftJsonSerializer Default = new NewtonsoftJsonSerializer(new ConverterContractResolver(Enumerable.Empty<JsonConverter>()));

        /// <summary>
        /// The <see cref="JsonSerializer"/> used by this <see cref="NewtonsoftJsonSerializer"/> instance.
        /// </summary>
        public JsonSerializer Serializer { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="NewtonsoftJsonSerializer"/> with a set of custom <see cref="JsonConverter"/> instances to be used by <see cref="ConverterContractResolver"/>.
        /// </summary>
        /// <param name="contractResolver">The underlying JSON.NET contract resolver.</param>
        public NewtonsoftJsonSerializer(IContractResolver contractResolver)
        {
            Serializer = new JsonSerializer
            {
                ContractResolver = contractResolver,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };
        }

        /// <summary>
        /// Serializes the object graph to the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> on which to serialize the object <paramref name="graph"/>.</param>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="type">The <see cref="Type"/> of object being serialized.</param>
        public void Serialize(Stream stream, Object graph, Type type)
        {
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
                Serializer.Serialize(jsonWriter, graph, type);
        }

        /// <summary>
        /// Deserialize an object graph from the speciied <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> from which to deserialize an object graph.</param>
        /// <param name="type">The <see cref="Type"/> of object being deserialized.</param>
        public Object Deserialize(Stream stream, Type type)
        {
            using (var streamReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true, bufferSize: 1024))
            using (var jsonReader = new JsonTextReader(streamReader))
                return Serializer.Deserialize(jsonReader, type);
        }
    }
}
