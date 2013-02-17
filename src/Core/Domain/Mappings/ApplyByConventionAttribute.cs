using System;
using System.Reflection;
using Spark.Infrastructure.Eventing;

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
    /// Indicates that aggregate event apply methods are mapped by convention (Default).
    /// </summary>
    /// <remarks>
    /// Methods must by of the form:
    /// <code>
    /// [public|protected] void Apply(Event e);
    /// </code>
    /// unless <see cref="MethodName"/>overriden.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ApplyByConventionAttribute : ApplyByReflectionAttribute
    {
        /// <summary>
        /// Gets or sets the case-insensitive apply method name (Default is <value>Apply</value>).
        /// </summary>
        public String MethodName { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ApplyByConventionAttribute"/>.
        /// </summary>
        public ApplyByConventionAttribute()
        {
            MethodName = "Apply";
        }
        
        /// <summary>
        /// Determine if the specified <paramref name="method"/> conforms to the configured apply method specification.
        /// </summary>
        /// <param name="method">The method info for the apply method candidate.</param>
        protected override Boolean MatchesApplyMethodDefinition(MethodInfo method)
        {
            var parameters = method.GetParameters();

            return method.ReturnParameter != null &&
                   method.ReturnParameter.ParameterType == typeof(void) &&
                   method.Name.Equals(MethodName, StringComparison.InvariantCultureIgnoreCase) &&
                   parameters.Length == 1 && parameters[0].ParameterType.DerivesFrom(typeof(Event));
        }
    }
}
