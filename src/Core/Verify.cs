using JetBrains.Annotations;
using System;
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
    internal static class Verify
    {
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
        /// <param name="value"></param>
        /// <param name="paramName"></param>
        [ContractAnnotation("value: null => halt")]
        public static void NotNullOrWhiteSpace(String value, [InvokerParameterName]String paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
  
            if(String.IsNullOrWhiteSpace(value))
                throw new ArgumentException(Messages.MustContainOneNonWhitespaceCharacter, paramName);
        }
    }
}
