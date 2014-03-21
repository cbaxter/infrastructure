using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

namespace Spark.Cqrs.Eventing.Mappings
{
    /// <summary>
    /// Base attribute for indicating event handle method mapping strategy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public abstract class HandleByStrategyAttribute : Attribute
    {
        private static readonly MethodInfo GetServiceMethod = typeof(IServiceProvider).GetMethod("GetService", new[] { typeof(Type) });

        /// <summary>
        /// Represents the default <see cref="HandleByStrategyAttribute"/> instance. This field is read-only.
        /// </summary>
        public static readonly HandleByStrategyAttribute Default = new HandleByConventionAttribute();

        /// <summary>
        /// Gets the handle method collection associated with the given event handler type.
        /// </summary>
        /// <param name="handlerType">The event handler type.</param>
        /// <param name="serviceProvider">The underlying service provider (IoC Container).</param>
        internal HandleMethodCollection GetHandleMethods(Type handlerType, IServiceProvider serviceProvider)
        {
            Verify.NotNull(handlerType, "handlerType");
            Verify.NotNull(serviceProvider, "serviceProvider");

            return MapHandleMethodsFor(handlerType, serviceProvider);
        }

        /// <summary>
        /// Creates the handle method collection for a given event handler type.
        /// </summary>
        /// <param name="handlerType">The event handler type.</param>
        /// <param name="serviceProvider">The underlying service provider (IoC Container).</param>
        protected abstract HandleMethodCollection MapHandleMethodsFor(Type handlerType, IServiceProvider serviceProvider);

        /// <summary>
        /// Compiles a handle method in to an invokable action.
        /// </summary>
        /// <param name="handleMethod">The reflected handle method.</param>
        /// <param name="eventType">The event type handled.</param>
        /// <param name="serviceProvider">The underlying service provider (IoC Container).</param>
        protected Action<Object, Event> CompileAction(MethodInfo handleMethod, Type eventType, IServiceProvider serviceProvider)
        {
            var eventParameter = Expression.Parameter(typeof(Event), "e");
            var eventHandlerParameter = Expression.Parameter(typeof(Object), "eventHandler");
            var body = Expression.Call(Expression.Convert(eventHandlerParameter, handleMethod.ReflectedType), handleMethod, GetMethodArguments(handleMethod, eventParameter, serviceProvider));
            var expression = Expression.Lambda<Action<Object, Event>>(body, new[] { eventHandlerParameter, eventParameter });

            return expression.Compile();
        }

        /// <summary>
        /// Get the set of method arguments to pass in to the underlying call expression.
        /// </summary>
        /// <param name="method">The method definition.</param>
        /// <param name="eventParameter">The event parameter reference.</param>
        /// <param name="serviceProvider">The underlying service provider (IoC Container).</param>
        private static IEnumerable<Expression> GetMethodArguments(MethodInfo method, ParameterExpression eventParameter, IServiceProvider serviceProvider)
        {
            var parameters = method.GetParameters();

            yield return Expression.TypeAs(eventParameter, parameters.First().ParameterType);

            foreach (var parameter in parameters.Skip(1))
            {
                var expression = parameter.GetCustomAttribute<TransientAttribute>() == null
                                     ? Expression.Constant(serviceProvider.GetService(parameter.ParameterType)) as Expression
                                     : Expression.Call(Expression.Constant(serviceProvider), GetServiceMethod, new Expression[] { Expression.Constant(parameter.ParameterType) });

                yield return Expression.Convert(expression, parameter.ParameterType);
            }
        }
    }
}
