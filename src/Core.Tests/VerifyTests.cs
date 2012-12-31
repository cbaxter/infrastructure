using System.Globalization;
using Spark.Infrastructure.Resources;
using System;
using Xunit;
using Xunit.Extensions;

/* Copyright (c) 2012 Spark Software Ltd.
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

namespace Spark.Infrastructure.Tests
{
    // ReSharper disable NotResolvedInText
    public static class UsingVerify
    {
        public class WhenCheckingForGreaterThan
        {
            [Fact]
            public void NullValuesThrowArgumentOutOfRangeException()
            {
                var expectedEx = new ArgumentOutOfRangeException("paramName", Exceptions.ArgumentNotGreaterThanValue.FormatWith(String.Empty));
                var actualEx = Assert.Throws<ArgumentOutOfRangeException>(() => Verify.GreaterThan(default(IComparable), null, "paramName"));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }

            [Fact]
            public void NullActualValueThrowArgumentOutOfRangeException()
            {
                var expectedEx = new ArgumentOutOfRangeException("paramName", Exceptions.ArgumentNotGreaterThanValue.FormatWith(0));
                var actualEx = Assert.Throws<ArgumentOutOfRangeException>(() => Verify.GreaterThan((Comparable)0, null, "paramName"));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }

            [Fact]
            public void NullExpectedValueDoesNotThrowException()
            {
                Assert.DoesNotThrow(() => Verify.GreaterThan(null, (Comparable)1, "paramName"));
            }

            [Fact]
            public void ActualLessThanExpectedThrowsArgumentOutOfRangeException()
            {
                var expectedEx = new ArgumentOutOfRangeException("paramName", 0, Exceptions.ArgumentNotGreaterThanValue.FormatWith(1));
                var actualEx = Assert.Throws<ArgumentOutOfRangeException>(() => Verify.GreaterThan(1, 0, "paramName"));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }

            [Fact]
            public void ActualEqualToExpectedThrowsArgumentOutOfRangeException()
            {
                var expectedEx = new ArgumentOutOfRangeException("paramName", 1, Exceptions.ArgumentNotGreaterThanValue.FormatWith(1));
                var actualEx = Assert.Throws<ArgumentOutOfRangeException>(() => Verify.GreaterThan(1, 1, "paramName"));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }

            [Fact]
            public void ActualGreaterThanExpectedDoesNotThrowException()
            {
                Assert.DoesNotThrow(() => Verify.GreaterThan(1, 2, "paramName"));
            }

            private class Comparable : IComparable
            {
                private readonly Int32 value;

                private Comparable(Int32 value)
                {
                    this.value = value;
                }

                public Int32 CompareTo(Object obj)
                {
                    return value.CompareTo(obj);
                }

                public override string ToString()
                {
                    return value.ToString(CultureInfo.InvariantCulture);
                }

                public static implicit operator Comparable(Int32 value)
                {
                    return new Comparable(value);
                }
            }
        }

        public class WhenCheckingForNull
        {
            [Fact]
            public void NotNullValueDoesNotThrow()
            {
                Assert.DoesNotThrow(() => Verify.NotNull(new Object(), "paramName"));
            }

            [Fact]
            public void NullValueThrowsArgumentNullException()
            {
                var expectedEx = new ArgumentNullException("paramName");
                var actualEx = Assert.Throws<ArgumentNullException>(() => Verify.NotNull(default(Object), "paramName"));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }
        }

        public class WhenCheckingForNullEmptyOrWhiteSpace
        {
            [Fact]
            public void NotEmptyValueDoesNotThrow()
            {
                Assert.DoesNotThrow(() => Verify.NotNullOrWhiteSpace("Value", "paramName"));
            }

            [Theory, InlineData(" "), InlineData("\r"), InlineData("\n"), InlineData("\r\n"), InlineData("\t")]
            public void WhiteSpaceValueThrowsArgumentException(String value)
            {
                var expectedEx = new ArgumentException(Exceptions.MustContainOneNonWhitespaceCharacter, "paramName");
                var actualEx = Assert.Throws<ArgumentException>(() => Verify.NotNullOrWhiteSpace(value, "paramName"));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }

            [Fact]
            public void EmptyValueThrowsArgumentException()
            {
                var expectedEx = new ArgumentException(Exceptions.MustContainOneNonWhitespaceCharacter, "paramName");
                var actualEx = Assert.Throws<ArgumentException>(() => Verify.NotNullOrWhiteSpace(String.Empty, "paramName"));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }

            [Fact]
            public void NullValueThrowsArgumentNullException()
            {
                var expectedEx = new ArgumentNullException("paramName");
                var ex = Assert.Throws<ArgumentNullException>(() => Verify.NotNullOrWhiteSpace(default(String), "paramName"));

                Assert.Equal(expectedEx.Message, ex.Message);
            }
        }
    }
}
