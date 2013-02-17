using System;
using JetBrains.Annotations;
using Spark.Infrastructure.Resources;

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

namespace Spark.Infrastructure
{
    /// <summary>
    /// Code contract utility class.
    /// </summary>
    public static class Verify
    {
        /// <summary>
        /// Throws an <exception cref="ArgumentException">ArgumentException</exception> if <paramref name="condition"/> is <value>false</value>.
        /// </summary>
        /// <param name="condition">The <see cref="Boolean"/> condition to check.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <param name="message">The exception message if <paramref name="condition"/> is <value>false</value>.</param>
        public static void True(Boolean condition, [InvokerParameterName] String paramName, String message)
        {
            if (!condition)
                throw new ArgumentException(message, paramName);
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentException">ArgumentException</exception> if <paramref name="condition"/> is <value>true</value>.
        /// </summary>
        /// <param name="condition">The <see cref="Boolean"/> condition to check.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <param name="message">The exception message if <paramref name="condition"/> is <value>true</value>.</param>
        public static void False(Boolean condition, [InvokerParameterName] String paramName, String message)
        {
            if (condition)
                throw new ArgumentException(message, paramName);
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentException">ArgumentException</exception> if <paramref name="type"/> does not derive from <paramref name="baseType"/>.
        /// </summary>
        /// <param name="baseType">The expected base type.</param>
        /// <param name="type">The type being checked.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        public static void TypeDerivesFrom(Type baseType, Type type, [InvokerParameterName] String paramName)
        {
            if (!type.DerivesFrom(baseType))
                throw new ArgumentException(Exceptions.TypeDoesNotDeriveFromBase.FormatWith(baseType, type), paramName);
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentOutOfRangeException">ArgumentOutOfRangeException</exception> if <paramref name="actual"/> is not equal to <paramref name="expected"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="expected"/> and <paramref name="actual"/>.</typeparam>
        /// <param name="expected">The value that <paramref name="actual"/> must equal.</param>
        /// <param name="actual">The value to check if not equal to <paramref name="expected"/>.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        public static void Equal<T>(T expected, T actual, [InvokerParameterName]String paramName)
          where T : IComparable
        {
            if (!Equals(expected, actual))
                throw new ArgumentOutOfRangeException(paramName, actual, Exceptions.ArgumentNotEqualToValue.FormatWith(expected, actual));
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentOutOfRangeException">ArgumentOutOfRangeException</exception> if <paramref name="actual"/> is equal to <paramref name="notExpected"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="notExpected"/> and <paramref name="actual"/>.</typeparam>
        /// <param name="notExpected">The value that <paramref name="actual"/> can not equal.</param>
        /// <param name="actual">The value to check if equal to <paramref name="notExpected"/>.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        public static void NotEqual<T>(T notExpected, T actual, [InvokerParameterName]String paramName)
          where T : IComparable
        {
            if (Equals(notExpected, actual))
                throw new ArgumentException(Exceptions.ArgumentEqualToValue.FormatWith(notExpected), paramName);
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentOutOfRangeException">ArgumentOutOfRangeException</exception> if <paramref name="actual"/> is less than or equal to <paramref name="exclusiveLowerBound"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="actual"/> and <paramref name="exclusiveLowerBound"/>.</typeparam>
        /// <param name="actual">The value to check if less than or equal to <paramref name="exclusiveLowerBound"/>.</param>
        /// <param name="exclusiveLowerBound">The exlusive lower bound for <paramref name="actual"/>.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        public static void GreaterThan<T>(T exclusiveLowerBound, T actual, [InvokerParameterName]String paramName)
          where T : IComparable
        {
            if (ReferenceEquals(actual, null) || actual.CompareTo(exclusiveLowerBound) <= 0)
                throw new ArgumentOutOfRangeException(paramName, actual, Exceptions.ArgumentNotGreaterThanValue.FormatWith(exclusiveLowerBound, actual));
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentOutOfRangeException">ArgumentOutOfRangeException</exception> if <paramref name="actual"/> is less than <paramref name="inclusiveLowerBound"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="actual"/> and <paramref name="inclusiveLowerBound"/>.</typeparam>
        /// <param name="actual">The value to check if less than <paramref name="inclusiveLowerBound"/>.</param>
        /// <param name="inclusiveLowerBound">The inclusive lower bound for <paramref name="actual"/>.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        public static void GreaterThanOrEqual<T>(T inclusiveLowerBound, T actual, [InvokerParameterName]String paramName)
          where T : IComparable
        {
            if (ReferenceEquals(actual, null) || actual.CompareTo(inclusiveLowerBound) < 0)
                throw new ArgumentOutOfRangeException(paramName, actual, Exceptions.ArgumentNotGreaterThanOrEqualToValue.FormatWith(inclusiveLowerBound, actual));
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentOutOfRangeException">ArgumentOutOfRangeException</exception> if <paramref name="actual"/> is greater than or equal to <paramref name="exclusiveUpperBound"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="actual"/> and <paramref name="exclusiveUpperBound"/>.</typeparam>
        /// <param name="actual">The value to check if greater than or equal to <paramref name="exclusiveUpperBound"/>.</param>
        /// <param name="exclusiveUpperBound">The exlusive upper bound for <paramref name="actual"/>.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        public static void LessThan<T>(T exclusiveUpperBound, T actual, [InvokerParameterName]String paramName)
          where T : IComparable
        {
            if (ReferenceEquals(actual, null) || actual.CompareTo(exclusiveUpperBound) >= 0)
                throw new ArgumentOutOfRangeException(paramName, actual, Exceptions.ArgumentNotLessThanValue.FormatWith(exclusiveUpperBound, actual));
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentOutOfRangeException">ArgumentOutOfRangeException</exception> if <paramref name="actual"/> is greater than <paramref name="inclusiveUpperBound"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="actual"/> and <paramref name="inclusiveUpperBound"/>.</typeparam>
        /// <param name="actual">The value to check if greater than <paramref name="inclusiveUpperBound"/>.</param>
        /// <param name="inclusiveUpperBound">The inclusive upper bound for <paramref name="actual"/>.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        public static void LessThanOrEqual<T>(T inclusiveUpperBound, T actual, [InvokerParameterName]String paramName)
          where T : IComparable
        {
            if (ReferenceEquals(actual, null) || actual.CompareTo(inclusiveUpperBound) > 0)
                throw new ArgumentOutOfRangeException(paramName, actual, Exceptions.ArgumentNotLessThanOrEqualToValue.FormatWith(inclusiveUpperBound, actual));
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentNullException">ArgumentNullException</exception> if <paramref name="value"/> is <value>null</value>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to check if null</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        [ContractAnnotation("value: null => halt")]
        public static void NotNull<T>(T value, [InvokerParameterName]String paramName)
          where T : class
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Throws an <exception cref="ArgumentNullException">ArgumentNullException</exception> if <paramref name="value"/> is <value>null</value> 
        /// or an <exception cref="ArgumentException">ArgumentException</exception> if <paramref name="value"/> is empty or whitespace only.
        /// </summary>
        /// <param name="value">The value to check if null, empty or white-space only.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        [ContractAnnotation("value: null => halt")]
        public static void NotNullOrWhiteSpace(String value, [InvokerParameterName]String paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);

            if (value.IsNullOrWhiteSpace())
                throw new ArgumentException(Exceptions.MustContainOneNonWhitespaceCharacter, paramName);
        }
    }
}
