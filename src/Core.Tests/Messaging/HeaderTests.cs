using System;
using Spark;
using Spark.Messaging;
using Spark.Resources;
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

namespace Test.Spark.Messaging
{
    namespace UsingHeader
    {
        public class WhenCreatingHeader
        {
            [Theory, InlineData(Header.Origin), InlineData(Header.RemoteAddress), InlineData(Header.Timestamp), InlineData(Header.UserAddress), InlineData(Header.UserName)]
            public void HeaderNameCannotBeReservedName(String name)
            {
                var expectedEx = new ArgumentException(Exceptions.ReservedHeaderName.FormatWith(name), nameof(name));
                var actualEx = Assert.Throws<ArgumentException>(() =>new Header(name, "value"));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }

            [Fact]
            public void HeaderNameCanBeReservedNameIfKeyValuePair()
            {
                Assert.Equal(Header.Timestamp, new Header(Header.Timestamp, SystemTime.Now.ToString(DateTimeFormat.RoundTrip), checkReservedNames: false).Name);
            }
        }

        public class WhenTestingEquality
        {
            [Fact]
            public void NameAndValueMustBeEqual()
            {
                var header1 = new Header("Header1", "Value");
                var header2 = new Header("Header1", "Value");

                Assert.True(header1.Equals(header2));
            }

            [Fact]
            public void HeaderCanBeBoxed()
            {
                var header1 = new Header("Header1", "Value");
                var header2 = new Header("Header1", "Value");

                Assert.True(header1.Equals((Object)header2));
            }
        }

        public class WhenGettingHashCode
        {
            [Fact]
            public void AlwaysReturnConsistentValue()
            {
                var header1 = new Header("Header1", "Value");
                var header2 = new Header("Header1", "Value");

                Assert.Equal(header1.GetHashCode(), header2.GetHashCode());
            }

            [Fact]
            public void CanGetHashCodeOfNullValue()
            {
                var header = new Header("Header", null);

                Assert.InRange(header.GetHashCode(), Int32.MinValue, Int32.MaxValue);
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnFriendlyDescription()
            {
                var header = new Header("Header", "Value");

                Assert.Equal("[Header,Value]", header.ToString());
            }
        }
    }
}
