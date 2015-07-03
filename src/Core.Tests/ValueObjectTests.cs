using System;
using System.Linq;
using System.Text.RegularExpressions;
using Spark;
using Xunit;

/* Copyright (c) 2014 Spark Software Ltd.
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

namespace Test.Spark
{
    namespace UsingValueObject
    {
        public class WhenCreatingValueObject
        {
            [Fact]
            public void ValueCannotBeNull()
            {
                Assert.Throws<ArgumentNullException>(() => new EmailAddress(null));
            }

            [Fact]
            public void ThrowFormatExceptionOnInvalidValue()
            {
                Assert.Throws<FormatException>(() => new EmailAddress("cbaxter"));
            }

            [Fact]
            public void FormatStringValueIfRequired()
            {
                Assert.Equal("cbaxter@sparksoftware.net", new EmailAddress(" CBaxter@SparkSoftware.net "));
            }

            [Fact]
            public void PassThroughPrimitiveValue()
            {
                var id = Guid.NewGuid();

                Assert.Equal(id, new TestId(id));
            }
        }

        public class WhenParsingValue
        {
            [Fact]
            public void TypeCannotBeNull()
            {
                Assert.Throws<ArgumentNullException>(() => ValueObject.Parse(null, "cbaxter@sparksoftware.net"));
            }

            [Fact]
            public void TypeMustDeriveFromValueObject()
            {
                Assert.Throws<ArgumentException>(() => ValueObject.Parse(typeof(Object), "cbaxter@sparksoftware.net"));
            }

            [Fact]
            public void ValueCannotBeNullOrWhiteSpaceOnly()
            {
                Assert.Throws<ArgumentNullException>(() => ValueObject.Parse(typeof(EmailAddress), null));
            }

            [Fact]
            public void ThrowFormatExceptionOnInvalidValue()
            {
                Assert.Throws<FormatException>(() => ValueObject.Parse(typeof(EmailAddress), "cbaxter"));
            }

            [Fact]
            public void RequireTryParseImplementationForNonStringValues()
            {
                Assert.Throws<NotSupportedException>(() => ValueObject.Parse(typeof(Unknown), "123"));
            }

            [Fact]
            public void FormatStringValueIfRequired()
            {
                Assert.Equal("cbaxter@sparksoftware.net", (EmailAddress)ValueObject.Parse(typeof(EmailAddress), " CBaxter@SparkSoftware.net "));
            }

            [Fact]
            public void PassThroughPrimitiveValue()
            {
                var id = Guid.NewGuid();

                Assert.Equal(id, (TestId)ValueObject.Parse(typeof(TestId), id.ToString()));
            }
        }

        public class WhenParsingWithGenericTypeParameter
        {
            [Fact]
            public void ValueCannotBeNullOrWhiteSpaceOnly()
            {
                Assert.Throws<ArgumentNullException>(() => ValueObject.Parse<EmailAddress>(null));
            }

            [Fact]
            public void ThrowFormatExceptionOnInvalidValue()
            {
                Assert.Throws<FormatException>(() => ValueObject.Parse<EmailAddress>("cbaxter"));
            }

            [Fact]
            public void FormatStringValueIfRequired()
            {
                Assert.Equal("cbaxter@sparksoftware.net", ValueObject.Parse<EmailAddress>(" CBaxter@SparkSoftware.net "));
            }

            [Fact]
            public void PassThroughPrimitiveValue()
            {
                var id = Guid.NewGuid();

                Assert.Equal(id, ValueObject.Parse<TestId>(id.ToString()));
            }
        }

        public class WhenAttemptingParseValue
        {
            [Fact]
            public void TypeCannotBeNull()
            {
                ValueObject result;
                Assert.Throws<ArgumentNullException>(() => ValueObject.TryParse(null, "cbaxter@sparksoftware.net", out result));
            }

            [Fact]
            public void TypeMustDeriveFromValueObject()
            {
                ValueObject result;
                Assert.Throws<ArgumentException>(() => ValueObject.TryParse(typeof(Object), "cbaxter@sparksoftware.net", out result));
            }

            [Fact]
            public void ValueCannotBeNullOrWhiteSpaceOnly()
            {
                ValueObject result;
                Assert.Throws<ArgumentNullException>(() => ValueObject.TryParse(typeof(EmailAddress), null, out result));
            }

            [Fact]
            public void ReturnFalseIfInvalidValue()
            {
                ValueObject result;
                Assert.False(ValueObject.TryParse(typeof(TestId), "cbaxter", out result));
                Assert.Null(result);
            }

            [Fact]
            public void ReturnTrueIfInvalidValue()
            {
                ValueObject result;
                Assert.True(ValueObject.TryParse(typeof(EmailAddress), "cbaxter@sparksoftware.net", out result));
                Assert.Equal("cbaxter@sparksoftware.net", (EmailAddress)result);
            }
        }

        public class WhenAttemptingParseWithGenericTypeParameter
        {
            [Fact]
            public void ValueCannotBeNullOrWhiteSpaceOnly()
            {
                EmailAddress result;
                Assert.Throws<ArgumentNullException>(() => ValueObject.TryParse(null, out result));
            }

            [Fact]
            public void ReturnTrueIfInvalidValue()
            {
                EmailAddress result;
                Assert.True(ValueObject.TryParse(" CBaxter@SparkSoftware.net ", out result));
                Assert.Equal("cbaxter@sparksoftware.net", result);
            }

            [Fact]
            public void ReturnFalseIfInvalidValue()
            {
                TestId result;
                Assert.False(ValueObject.TryParse("cbaxter", out result));
                Assert.Null(result);
            }
        }

        public class WhenComparingValues
        {
            [Fact]
            public void CanCompareTypedValueToObject()
            {
                var email = new EmailAddress("cbaxter@sparksoftware.net");

                Assert.Equal(0, email.CompareTo((Object)"cbaxter@sparksoftware.net"));
            }

            [Fact]
            public void CanCompareTypedValueToRawValue()
            {
                var email = new EmailAddress("cbaxter@sparksoftware.net");

                Assert.Equal(0, email.CompareTo("cbaxter@sparksoftware.net"));
            }

            [Fact]
            public void CanCompareTypedValueToAnotherInstance()
            {
                var lhs = new EmailAddress("cbaxter@sparksoftware.net");
                var rhs = new EmailAddress("cbaxter@sparksoftware.net");

                Assert.Equal(0, lhs.CompareTo(rhs));
            }

            [Fact]
            public void SortByRawValue()
            {
                var a = new EmailAddress("a@b.ca");
                var b = new EmailAddress("b@b.ca");
                var c = new EmailAddress("c@b.ca");

                var sorted = new[] { c, a, b }.OrderBy(value => value).ToArray();

                Assert.Equal(a, sorted[0]);
                Assert.Equal(b, sorted[1]);
                Assert.Equal(c, sorted[2]);
            }
        }

        public class WhenTestingEquality
        {
            [Fact]
            public void CanEqualValueAsObject()
            {
                var email = new EmailAddress("cbaxter@sparksoftware.net");

                // ReSharper disable SuspiciousTypeConversion.Global
                Assert.True(email.Equals((Object)"cbaxter@sparksoftware.net"));
                // ReSharper restore SuspiciousTypeConversion.Global
            }

            [Fact]
            public void CanEqualRawValue()
            {
                var email = new EmailAddress("cbaxter@sparksoftware.net");

                Assert.True(email.Equals("cbaxter@sparksoftware.net"));
            }

            [Fact]
            public void CanEqualAnotherInstanceOfTypedValue()
            {
                var lhs = new EmailAddress("cbaxter@sparksoftware.net");
                var rhs = new EmailAddress("cbaxter@sparksoftware.net");

                Assert.True(lhs.Equals(rhs));
            }
        }

        public class WhenGettingHashCode
        {
            [Fact]
            public void HashCodeDiffersByType()
            {
                var a = new EmailAddress("cbaxter@sparksoftware.net");
                var b = new StringType("cbaxter@sparksoftware.net");

                Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void UseRawValueByDefault()
            {
                var email = new EmailAddress("cbaxter@sparksoftware.net");

                Assert.Equal("cbaxter@sparksoftware.net", email.ToString());
            }
        }

        public sealed class TestId : ValueObject<Guid>
        {
            public TestId()
                : base(GuidStrategy.NewGuid())
            { }

            public TestId(Guid value)
                : base(value)
            { }

            protected override Boolean TryParse(String value, out Guid result)
            {
                return Guid.TryParse(value, out result);
            }
        }

        public sealed class Unknown : ValueObject<Int32>
        {
            public Unknown()
                : base(0)
            { }
        }

        public sealed class StringType : ValueObject<String>
        {
            public StringType(String value)
                : base(value)
            { }
        }

        public sealed class EmailAddress : ValueObject<String>
        {
            private static readonly Regex EmailPattern = new Regex(@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

            public EmailAddress(String email)
                : base(email)
            { }

            protected override Boolean TryGetValue(String value, out String result)
            {
                var match = EmailPattern.Match((value ?? String.Empty).Trim().ToLowerInvariant());

                result = match.Success ? match.Value : null;

                return result != null;
            }
        }
    }
}
