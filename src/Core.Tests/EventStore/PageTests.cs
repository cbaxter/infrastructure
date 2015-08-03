using System;
using Spark;
using Spark.EventStore;
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

namespace Test.Spark.EventStore
{
    // ReSharper disable NotResolvedInText
    namespace UsingPage
    {
        public class WhenCreatingPage
        {
            [Fact]
            public void SkipMustBeGreaterThanOrEqualToZero()
            {
                var expectedEx = new ArgumentOutOfRangeException("skip", -1, Exceptions.ArgumentNotGreaterThanOrEqualToValue.FormatWith(0));
                var actualEx = Assert.Throws<ArgumentOutOfRangeException>(() => new Page(-1, 10));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }

            [Fact]
            public void TakeMustBeGreaterThanZero()
            {
                var expectedEx = new ArgumentOutOfRangeException("take", 0, Exceptions.ArgumentNotGreaterThanValue.FormatWith(0));
                var actualEx = Assert.Throws<ArgumentOutOfRangeException>(() => new Page(0, 0));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }
        }

        public class WhenGettingNextPage
        {
            [Fact]
            public void SkipIncrementsByTake()
            {
                var currentPage = new Page(0, 100);
                var nextPage = currentPage.NextPage();

                Assert.Equal(100, nextPage.Skip);
            }

            [Fact]
            public void TakeRemainsUnchanged()
            {
                var currentPage = new Page(0, 100);
                var nextPage = currentPage.NextPage();

                Assert.Equal(100, nextPage.Take);
            }
        }

        public class WhenTestingEquality
        {
            [Fact]
            public void SkipAndTakeMustBeEqual()
            {
                var page1 = new Page(0, 100);
                var page2 = new Page(0, 100);

                Assert.True(page1.Equals(page2));
            }

            [Fact]
            public void PageCanBeBoxed()
            {
                var page1 = new Page(0, 100);
                var page2 = new Page(0, 100);

                Assert.True(page1.Equals((Object)page2));
            }
        }

        public class WhenGettingHashCode
        {
            [Fact]
            public void AlwaysReturnConsistentValue()
            {
                var page1 = new Page(0, 100);
                var page2 = new Page(0, 100);

                Assert.Equal(page1.GetHashCode(), page2.GetHashCode());
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnFriendlyDescription()
            {
                var page = new Page(0, 100);

                Assert.Equal("0 - 100", page.ToString());
            }
        }
    }
}
