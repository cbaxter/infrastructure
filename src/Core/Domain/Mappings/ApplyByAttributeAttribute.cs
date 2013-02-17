using System;
using System.Reflection;
using Spark.Infrastructure.Eventing;
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

namespace Spark.Infrastructure.Domain.Mappings
{    
    /// <summary>
    /// Indicates that aggregate event apply methods are mapped by explicit usage of <see cref="ApplyMethodAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Methods must by of the form:
    /// <code>
    /// [ApplyMethod] 
    /// [public|protected] void MethodName(Event e);
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ApplyByAttributeAttribute : ApplyByReflectionAttribute
    {
        /// <summary>
        /// Determine if the specified <paramref name="method"/> conforms to the configured apply method specification.
        /// </summary>
        /// <param name="method">The method info for the apply method candidate.</param>
        protected override Boolean MatchesApplyMethodDefinition(MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<ApplyMethodAttribute>();

            if (attribute == null)
                return false;

            if (method.ReturnParameter == null || method.ReturnParameter.ParameterType != typeof(void))
                throw new MappingException(Exceptions.AggregateApplyMethodMustHaveVoidReturn.FormatWith(method.ReflectedType, method.Name));

            var parameters = method.GetParameters();
            if (parameters.Length != 1 || !parameters[0].ParameterType.DerivesFrom(typeof(Event)))
                throw new MappingException(Exceptions.AggregateApplyMethodInvalidParameters.FormatWith(typeof(Event), method.ReflectedType, method.Name));

            return true;
        }
    }
}
