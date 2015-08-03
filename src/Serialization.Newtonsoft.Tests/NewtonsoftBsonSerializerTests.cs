using System;
using System.Collections.Generic;
using System.IO;
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

namespace Test.Spark.Serialization
{
    namespace UsingNewtonsoftBsonSerializer
    {
        public class WhenSerializingData
        {
            [Fact]
            public void IncludeTypeWhenDeclaringTypeDoesNotMatchSpecifiedType()
            {
                String json;
                using (var memoryStream = new MemoryStream())
                {
                    NewtonsoftBsonSerializer.Default.Serialize(memoryStream, new Dictionary<String, Object> { { "key", "value" } }, typeof(Object));

                    json = Convert.ToBase64String(memoryStream.ToArray());
                }

                Assert.Equal("hgAAAAIkdHlwZQBnAAAAU3lzdGVtLkNvbGxlY3Rpb25zLkdlbmVyaWMuRGljdGlvbmFyeWAyW1tTeXN0ZW0uU3RyaW5nLCBtc2NvcmxpYl0sW1N5c3RlbS5PYmplY3QsIG1zY29ybGliXV0sIG1zY29ybGliAAJrZXkABgAAAHZhbHVlAAA=", json);
            }
        }

        public class WhenDeserializingData
        {
            [Fact]
            public void UseIncludedTypeWhenSpecified()
            {
                Byte[] json = Convert.FromBase64String("hgAAAAIkdHlwZQBnAAAAU3lzdGVtLkNvbGxlY3Rpb25zLkdlbmVyaWMuRGljdGlvbmFyeWAyW1tTeXN0ZW0uU3RyaW5nLCBtc2NvcmxpYl0sW1N5c3RlbS5PYmplY3QsIG1zY29ybGliXV0sIG1zY29ybGliAAJrZXkABgAAAHZhbHVlAAA=");
                IDictionary<String, Object> graph;

                using (var memoryStream = new MemoryStream(json, writable: false))
                    graph = (IDictionary<String,Object>) NewtonsoftBsonSerializer.Default.Deserialize(memoryStream, typeof(Object));

                Assert.Equal("value", graph["key"]);
            }
        }
    }
}
