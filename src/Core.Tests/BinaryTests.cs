using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

namespace Test.Spark
{
    namespace UsingBinary
    {
        public class WhenCreatingBinary
        {
            [Fact]
            public void WrapUnderlyingBytes()
            {
                var rawBytes = new Byte[0];
                var bytes = new Binary(rawBytes);

                Assert.Same(rawBytes, (Byte[])bytes);
            }
        }

        public class WhenComparingValues
        {
            [Fact]
            public void CanCompareToBoxedBytes()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3 });
                var rhs = new Binary(new Byte[] { 1, 2, 3 });

                Assert.Equal(0, lhs.CompareTo((Object)rhs));
            }

            [Fact]
            public void CanCompareToBoxedRawBytes()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3 });
                var rhs = new Byte[] { 1, 2, 3 };

                Assert.Equal(0, lhs.CompareTo((Object)rhs));
            }

            [Fact]
            public void CanCompareToRawBytes()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3 });
                var rhs = new Binary(new Byte[] { 1, 2, 3 });

                Assert.Equal(0, lhs.CompareTo((Byte[])rhs));
            }

            [Fact]
            public void CanCompareToOtherBytes()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3 });
                var rhs = new Binary(new Byte[] { 1, 2, 3 });

                Assert.Equal(0, lhs.CompareTo(rhs));
            }

            [Fact]
            public void SortByBytesFromLeftToRight()
            {
                var first = new Binary(new Byte[] { 1, 2, 3 });
                var second = new Binary(new Byte[] { 1, 2, 3, 4 });
                var third = new Binary(new Byte[] { 1, 2, 4 });
                var fourth = new Binary(new Byte[] { 2, 3, 4 });
                var sorted = new[] { fourth, default(Binary), third, first, second }.OrderBy(value => value).ToArray();

                Assert.Null(sorted[0]);
                Assert.Same(first, sorted[1]);
                Assert.Same(second, sorted[2]);
                Assert.Same(third, sorted[3]);
                Assert.Same(fourth, sorted[4]);
            }
        }

        public class WhenTestingEquality
        {
            [Fact]
            public void ValueCannotBeNull()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3 });

                Assert.False(lhs.Equals(default(Binary)));
            }

            [Fact]
            public void LengthsMustBeEqual()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3 });
                var rhs = new Binary(new Byte[] { 1, 2, 3, 4 });

                Assert.False(lhs.Equals(rhs));
            }

            [Fact]
            public void AllBytesMustBeEqual()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3, 4 });
                var rhs = new Binary(new Byte[] { 1, 2, 2, 4 });

                Assert.False(lhs.Equals(rhs));
            }

            [Fact]
            public void CanUseReferenceEquality()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3 });

                Assert.True(lhs.Equals(lhs));
            }

            [Fact]
            public void CanEqualBoxedBytes()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3 });
                var rhs = new Binary(new Byte[] { 1, 2, 3 });

                Assert.True(lhs.Equals((Object)rhs));
            }

            [Fact]
            public void CanEqualBoxedRawBytes()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3 });
                var rhs = new Byte[] { 1, 2, 3 };

                Assert.True(lhs.Equals((Object)rhs));
            }

            [Fact]
            public void CanEqualRawBytes()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3 });
                var rhs = new Binary(new Byte[] { 1, 2, 3 });

                Assert.True(lhs.Equals((Byte[])rhs));
            }

            [Fact]
            public void CanEqualOtherBytes()
            {
                var lhs = new Binary(new Byte[] { 1, 2, 3 });
                var rhs = new Binary(new Byte[] { 1, 2, 3 });

                Assert.True(lhs.Equals(rhs));
            }
        }

        public class WhenCalculatingHashCode
        {
            [Fact]
            public void RecalculateOnEachCall()
            {
                var rawBytes = new Byte[] { 1, 2, 3, 4 };
                var originalHash = new Binary(rawBytes).GetHashCode();

                rawBytes[2] = 5;

                Assert.NotEqual(originalHash, new Binary(rawBytes).GetHashCode());
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void DisplayRawHexValues()
            {
                var bytes = new Binary(new Byte[] { 1, 2, 3, 4 });

                Assert.Equal("0x01020304", bytes.ToString());
            }
        }

        public class WhenParsingString
        {
            [Fact]
            public void HexStringMustNotBeNull()
            {
                Assert.Throws<FormatException>(() => Binary.Parse(null));
            }

            [Fact]
            public void HexStringMustHaveAnEvenLength()
            {
                Assert.Throws<FormatException>(() => Binary.Parse("01020304F"));
            }

            [Fact]
            public void HexStringCannotContainNonHexCharacters()
            {
                Assert.Throws<FormatException>(() => Binary.Parse("01020304FX"));
            }


            [Fact]
            public void HexPreambleOptional()
            {
                var expected = new Binary(new Byte[] { 1, 2, 3, 4, 255 });
                var actual = Binary.Parse("01020304FF");

                Assert.Equal(expected, actual);
            }

            [Fact]
            public void WillIgnoreHexPreamble()
            {
                var expected = new Binary(new Byte[] { 1, 2, 3, 4, 255 });
                var actual = Binary.Parse("0x01020304FF");

                Assert.Equal(expected, actual);
            }
        }

        public class WhenEnumeratingBytes
        {
            [Fact]
            public void CanEnumerateBoxedValues()
            {
                var bytes = (IEnumerable)new Binary(new Byte[] { 1, 2, 3, 4 });

                Assert.Equal(4, bytes.Cast<Byte>().Count());
            }

            [Fact]
            public void CanEnumerateStronglyTypedValues()
            {
                var bytes = (IEnumerable<Byte>)new Binary(new Byte[] { 1, 2, 3, 4 });

                Assert.Equal(4, bytes.Count());
            }
        }
    }
}
