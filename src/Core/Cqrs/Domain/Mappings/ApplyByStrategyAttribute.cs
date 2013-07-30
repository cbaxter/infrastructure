using System;
using System.Linq.Expressions;
using System.Reflection;
using Spark.Cqrs.Eventing;

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

namespace Spark.Cqrs.Domain.Mappings
{
    /// <summary>
    /// Base attribute for indicating apply method mapping strategy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public abstract class ApplyByStrategyAttribute : Attribute
    {
        public static readonly ApplyByStrategyAttribute Default = new ApplyByConventionAttribute();

        /// <summary>
        /// Gets or sets whether or not an event apply method is optional (Default is <value>true</value>).
        /// </summary>
        public Boolean ApplyOptional { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ApplyByStrategyAttribute"/>.
        /// </summary>
        protected ApplyByStrategyAttribute()
        {
            ApplyOptional = true;
        }

        /// <summary>
        /// Gets the apply method collection associated with the given aggregate type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        internal ApplyMethodCollection GetApplyMethods(Type aggregateType)
        {
            Verify.NotNull(aggregateType, "aggregateType");
            Verify.TypeDerivesFrom(typeof(Aggregate), aggregateType, "aggregateType");

            return MapApplyMethodsFor(aggregateType);
        }

        /// <summary>
        /// Creates the apply method collection for a given aggregate type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        protected abstract ApplyMethodCollection MapApplyMethodsFor(Type aggregateType);

        /// <summary>
        /// Compiles an apply method in to an invokable action.
        /// </summary>
        /// <param name="applyMethod">The reflected apply method.</param>
        /// <param name="eventType">The event type handled.</param>
        protected Action<Aggregate, Event> CompileAction(MethodInfo applyMethod, Type eventType)
        {
            var eventParameter = Expression.Parameter(typeof(Event), "e");
            var aggregateParameter = Expression.Parameter(typeof(Aggregate), "aggregate");
            var arguments = new Expression[] { Expression.Convert(eventParameter, eventType) };
            var body = Expression.Call(Expression.Convert(aggregateParameter, applyMethod.ReflectedType), applyMethod, arguments);
            var expression = Expression.Lambda<Action<Aggregate, Event>>(body, new[] { aggregateParameter, eventParameter });

            return expression.Compile();
        }
    }
}
