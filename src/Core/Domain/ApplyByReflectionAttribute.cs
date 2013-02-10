using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Spark.Infrastructure.Domain
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public abstract class ApplyByReflectionAttribute : ApplyLocatorAttribute
    {
        /// <summary>
        /// Gets or sets whether non-public apply methods will be included in apply method search.
        /// </summary>
        public Boolean PublicOnly { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ApplyByReflectionAttribute"/>.
        /// </summary>
        protected ApplyByReflectionAttribute()
        {
            PublicOnly = false;
        }

        /// <summary>
        /// Maps the apply methods on the specified type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type on which to locate apply methods.</param>
        protected override ApplyMethodCollection MapApplyMethodsFor(Type aggregateType)
        {
            var bindingFlags = GetBindingFlags(PublicOnly);
            var applyMethods = aggregateType.GetMethods(bindingFlags).Where(MatchesApplyMethodDefinition);
            var mappings = new Dictionary<Type, Action<Aggregate, Event>>();

            foreach (var applyMethod in applyMethods)
            {
                var eventType = applyMethod.GetParameters().Single().ParameterType;
                var compiledAction = CompileAction(applyMethod, eventType);

                mappings.Add(eventType, compiledAction);
            }

            return new ApplyMethodCollection(ApplyOptional, mappings);
        }
        
        /// <summary>
        /// Gets the reflection binding flags to be used when locating apply methods.
        /// </summary>
        /// <param name="publicOnly">True to exclude <see cref="BindingFlags.NonPublic"/>; otherwise false.</param>
        private static BindingFlags GetBindingFlags(Boolean publicOnly)
        {
            var bindingFlags = BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase | BindingFlags.Public;

            if (!publicOnly)
                bindingFlags |= BindingFlags.NonPublic;

            return bindingFlags;
        }
        
        /// <summary>
        /// Determine if the specified <paramref name="method"/> conforms to the configured apply method specification.
        /// </summary>
        /// <param name="method">The method info for the apply method candidate.</param>
        protected abstract Boolean MatchesApplyMethodDefinition(MethodInfo method);
    }
}
