using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;

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
    /// Serializes or Deserializes an object graph to or from the provided <see cref="Stream"/> using a <see cref="BinaryFormatter"/>.
    /// </summary>
    public sealed class DeflateSerializer : ISerializeObjects
    {
        private readonly ISerializeObjects serializer;

        /// <summary>
        /// Initializes a new instance of <see cref="GZipSerializer"/>.
        /// </summary>
        /// <param name="serializer">The base serializer to wrap with GZip compression.</param>
        public DeflateSerializer(ISerializeObjects serializer)
        {
            Verify.NotNull(serializer, "serializer");

            this.serializer = serializer;
        }

        /// <summary>
        /// Serializes the object graph to the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> on which to serialize the object <paramref name="graph"/>.</param>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="type">The <see cref="Type"/> of object being serialized.</param>
        public void Serialize(Stream stream, Object graph, Type type)
        {
            Verify.NotNull(graph, "graph");
            Verify.NotNull(stream, "stream");

            using (var deflateStream = new DeflateStream(stream, CompressionMode.Compress))
                serializer.Serialize(deflateStream, graph, type);
        }

        /// <summary>
        /// Deserialize an object graph from the speciied <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> from which to deserialize an object graph.</param>
        /// <param name="type">The <see cref="Type"/> of object being deserialized.</param>
        public Object Deserialize(Stream stream, Type type)
        {
            Verify.NotNull(stream, "stream");

            using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
                return serializer.Deserialize(deflateStream, type);
        }
    }
}
