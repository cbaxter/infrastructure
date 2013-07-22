using System;
using System.Reflection;
using Spark.Infrastructure.Domain;
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

namespace Spark.Infrastructure.Eventing.Mappings
{
    /// <summary>
    /// Indicates that event handler methods are mapped by explicit usage of <see cref="Domain.Mappings.HandleMethodAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Methods must by of the form:
    /// <code>
    /// [HandleMethod] 
    /// [public|protected] void MethodName(Event e[, Object service, ...]);
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class HandleByAttributeAttribute : HandleByReflectionAttribute
    {
        /// <summary>
        /// Determine if the specified <paramref name="method"/> conforms to the configured handle method specification.
        /// </summary>
        /// <param name="method">The method info for the handle method candidate.</param>
        protected override Boolean MatchesHandleMethodDefinition(MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<HandleMethodAttribute>();

            if (attribute == null)
                return false;

            if (method.ReturnParameter == null || method.ReturnParameter.ParameterType != typeof(void))
                throw new MappingException(Exceptions.HandleMethodMustHaveVoidReturn.FormatWith(method.ReflectedType, method.Name));

            var parameters = method.GetParameters();
            if (parameters.Length == 0 || !parameters[0].ParameterType.DerivesFrom(typeof(Event)))
                throw new MappingException(Exceptions.HandleMethodInvalidParameters.FormatWith(typeof(Event), method.ReflectedType, method.Name));

            return true;
        }
    }
}
