using System;
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
    namespace UsingDeflateSerializer
    {
        public sealed class WhenSerializingStream
        {
            [Fact]
            public void CompressBaseSerializerOutput()
            {
                var baseSerializer = new BinarySerializer();
                var gzipSerializer = new DeflateSerializer(baseSerializer);

                using (var memoryStream = new MemoryStream())
                {
                    gzipSerializer.Serialize(memoryStream, "My Object", typeof(String));
                    Assert.Equal("Y2BkYGD4DwQgGgTYQAxO30oF/6Ss1OQSbgA=", Convert.ToBase64String(memoryStream.ToArray()));
                }
            }
        }

        public sealed class WhenDeserializingStream
        {
            [Fact]
            public void DecompressBaseSerializerOutput()
            {
                var baseSerializer = new BinarySerializer();
                var gzipSerializer = new DeflateSerializer(baseSerializer);

                using (var memoryStream = new MemoryStream(Convert.FromBase64String("Y2BkYGD4DwQgGgTYQAxO30oF/6Ss1OQSbgA=")))
                {
                    Assert.Equal("My Object", gzipSerializer.Deserialize(memoryStream, typeof(String)));
                }
            }
        }
    }
}
