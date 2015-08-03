using System;
using Spark;
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
    namespace UsingBinaryConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(default(Binary));

                Validate(json, "null");
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var binary = new Binary(Guid.Parse("a6c45a28-c572-4d5b-ac18-7b0ec2d723fb").ToByteArray());
                var json = WriteJson(binary);

                Validate(json, "\"KFrEpnLFW02sGHsOwtcj+w==\"");
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<Binary>("null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var expected = new Binary(Guid.Parse("a6c45a28-c572-4d5b-ac18-7b0ec2d723fb").ToByteArray());
                var actual = ReadJson<Binary>("\"KFrEpnLFW02sGHsOwtcj+w==\"");

                Assert.Equal(expected, actual);
            }
        }

        public class WhenWritingBson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeToBson()
            {
                var binary = new Binary(Guid.Parse("a6c45a28-c572-4d5b-ac18-7b0ec2d723fb").ToByteArray());
                var bson = WriteBson(new BinaryContainer { Value = binary });

                Validate(bson, "IQAAAAVWYWx1ZQAQAAAAAChaxKZyxVtNrBh7DsLXI/sA");
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var expected = new Binary(Guid.Parse("a6c45a28-c572-4d5b-ac18-7b0ec2d723fb").ToByteArray());
                var bson = "IQAAAAV2YWx1ZQAQAAAAAChaxKZyxVtNrBh7DsLXI/sA";
                var actual = ReadBson<BinaryContainer>(bson);

                Assert.Equal(expected, actual.Value);
            }
        }

        public class BinaryContainer
        {
            public Binary Value { get; set; }
        }
    }
}
