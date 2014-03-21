using System;
using Spark;
using Xunit;
using Xunit.Extensions;

/* Copyright (c) 2013 Spark Software Ltd.
 * 
 * This source is subject to the GNU Lesser General Public License.
 * See: http://www.gnu.org/copyleft/lesser.html
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE. 
 */

#pragma warning disable 1720
namespace Test.Spark
{
    public static class UsingStringExtensions
    {
        public class WhenEnsuringNotNull
        {
            [Fact]
            public void ReturnSameValueIfNotNull()
            {
                const String value = "MyTestString";

                Assert.Same(value, value.EmptyIfNull());
            }

            [Fact]
            public void ReturnEmptyIfNull()
            {
                Assert.Same(String.Empty, default(String).EmptyIfNull());
            }
        }

        public class WhenFormattingString
        {
            [Fact]
            public void CanFormatWithOneArgument()
            {
                Assert.Equal("arg0 = 0", "arg0 = {0}".FormatWith(0));
            }

            [Fact]
            public void CanFormatWithTwoArguments()
            {
                Assert.Equal("arg0 = 0, arg1 = 1", "arg0 = {0}, arg1 = {1}".FormatWith(0, 1));
            }

            [Fact]
            public void CanFormatWithThreeArguments()
            {
                Assert.Equal("arg0 = 0, arg1 = 1, arg2 = 2", "arg0 = {0}, arg1 = {1}, arg2 = {2}".FormatWith(0, 1, 2));
            }

            [Fact]
            public void CanFormatWithMoreThanThreeArguments()
            {
                Assert.Equal("arg0 = 0, arg1 = 1, arg2 = 2, arg3 = 3", "arg0 = {0}, arg1 = {1}, arg2 = {2}, arg3 = {3}".FormatWith(0, 1, 2, 3));
            }
        }

        public class WhenCheckingIfNull
        {
            [Fact]
            public void ReturnTrueIfNull()
            {
                Assert.True(default(String).IsNull());
            }

            [Fact]
            public void ReturnFalseIfNotNull()
            {
                Assert.False(String.Empty.IsNull());
            }
        }

        public class WhenCheckingIfNotNull
        {
            [Fact]
            public void ReturnFalseIfNull()
            {
                Assert.False(default(String).IsNotNull());
            }

            [Fact]
            public void ReturnTrueIfNotNull()
            {
                Assert.True(String.Empty.IsNotNull());
            }
        }

        public class WhenCheckingIfEmpty
        {
            [Fact]
            public void ReturnTrueIfEmpty()
            {
                Assert.True(String.Empty.IsEmpty());
            }

            [Theory, InlineData(null), InlineData(" "), InlineData("\r"), InlineData("\n"), InlineData("\r\n"), InlineData("\t"), InlineData("MyTestString")]
            public void ReturnFalseIfNotEmpty(String value)
            {
                Assert.False(value.IsEmpty());
            }
        }

        public class WhenCheckingIfNotEmpty
        {
            [Fact]
            public void ReturnFalseIfEmpty()
            {
                Assert.False(String.Empty.IsNotEmpty());
            }

            [Theory, InlineData(null), InlineData(" "), InlineData("\r"), InlineData("\n"), InlineData("\r\n"), InlineData("\t"), InlineData("MyTestString")]
            public void ReturnTrueIfNotEmpty(String value)
            {
                Assert.True(value.IsNotEmpty());
            }
        }

        public class WhenCheckingIfNullOrEmpty
        {
            [Fact]
            public void ReturnTrueIfNull()
            {
                Assert.True(default(String).IsNullOrEmpty());
            }

            [Fact]
            public void ReturnTrueIfEmpty()
            {
                Assert.True(String.Empty.IsNullOrEmpty());
            }

            [Theory, InlineData(" "), InlineData("\r"), InlineData("\n"), InlineData("\r\n"), InlineData("\t"), InlineData("MyTestString")]
            public void ReturnFalseIfNotNullOrEmpty(String value)
            {
                Assert.False(value.IsNullOrEmpty());
            }
        }

        public class WhenCheckingIfNotNullOrEmpty
        {
            [Fact]
            public void ReturnFalseIfNull()
            {
                Assert.False(default(String).IsNotNullOrEmpty());
            }

            [Fact]
            public void ReturnFalseIfEmpty()
            {
                Assert.False(String.Empty.IsNotNullOrEmpty());
            }

            [Theory, InlineData(" "), InlineData("\r"), InlineData("\n"), InlineData("\r\n"), InlineData("\t"), InlineData("MyTestString")]
            public void ReturnTrueIfNotNullOrEmpty(String value)
            {
                Assert.True(value.IsNotNullOrEmpty());
            }
        }

        public class WhenCheckingIfNullOrWhiteSpace
        {
            [Fact]
            public void ReturnTrueIfNull()
            {
                Assert.True(default(String).IsNullOrWhiteSpace());
            }

            [Fact]
            public void ReturnTrueIfEmpty()
            {
                Assert.True(String.Empty.IsNullOrWhiteSpace());
            }

            [Theory, InlineData(" "), InlineData("\r"), InlineData("\n"), InlineData("\r\n"), InlineData("\t")]
            public void ReturnTrueIfWhiteSpace(String value)
            {
                Assert.True(value.IsNullOrWhiteSpace());
            }

            [Fact]
            public void ReturnFalseIfNotNullOrEmpty()
            {
                Assert.False("MyTestString".IsNullOrWhiteSpace());
            }
        }

        public class WhenCheckingIfNotNullOrWhiteSpace
        {
            [Fact]
            public void ReturnFalseIfNull()
            {
                Assert.False(default(String).IsNotNullOrWhiteSpace());
            }

            [Fact]
            public void ReturnFalseIfEmpty()
            {
                Assert.False(String.Empty.IsNotNullOrWhiteSpace());
            }

            [Theory, InlineData(" "), InlineData("\r"), InlineData("\n"), InlineData("\r\n"), InlineData("\t")]
            public void ReturnFalseIfWhiteSpace(String value)
            {
                Assert.False(value.IsNotNullOrWhiteSpace());
            }

            [Fact]
            public void ReturnTrueIfNotNullOrEmpty()
            {
                Assert.True("MyTestString".IsNotNullOrWhiteSpace());
            }
        }

        public class WhenConvertingStringToMd5Hash
        {
            [Fact]
            public void CaseSensitiveByDefault()
            {
                var value = "My Cast Sensitive String";

                Assert.NotEqual(value.ToGuid(), value.ToLowerInvariant().ToGuid());
            }
            
            [Fact]
            public void CanBeCaseInsensitiveIfDesired()
            {
                var value = "My Cast Sensitive String";

                Assert.Equal(value.ToGuid(ignoreCase: true), value.ToLowerInvariant().ToGuid(ignoreCase: true));
            }
        }
    }
}
#pragma warning restore 1720
