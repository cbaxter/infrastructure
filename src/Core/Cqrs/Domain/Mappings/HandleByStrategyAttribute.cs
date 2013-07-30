using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Spark.Cqrs.Commanding;

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
    /// Base attribute for indicating handle method mapping strategy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public abstract class HandleByStrategyAttribute : Attribute
    {
        private static readonly MethodInfo GetServiceMethod = typeof(IServiceProvider).GetMethod("GetService", new[] { typeof(Type) });
        public static readonly HandleByStrategyAttribute Default = new HandleByConventionAttribute();

        /// <summary>
        /// Gets the handle method collection associated with the given aggregate type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        /// <param name="serviceProvider">The underlying service provider (IoC Container).</param>
        internal HandleMethodCollection GetHandleMethods(Type aggregateType, IServiceProvider serviceProvider)
        {
            Verify.NotNull(aggregateType, "aggregateType");
            Verify.NotNull(serviceProvider, "serviceProvider");
            Verify.TypeDerivesFrom(typeof(Aggregate), aggregateType, "aggregateType");

            return MapHandleMethodsFor(aggregateType, serviceProvider);
        }

        /// <summary>
        /// Creates the handle method collection for a given aggregate type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        /// <param name="serviceProvider">The underlying service provider (IoC Container).</param>
        protected abstract HandleMethodCollection MapHandleMethodsFor(Type aggregateType, IServiceProvider serviceProvider);

        /// <summary>
        /// Compiles a handle method in to an invokable action.
        /// </summary>
        /// <param name="handleMethod">The reflected handle method.</param>
        /// <param name="commandType">The command type handled.</param>
        /// <param name="serviceProvider">The underlying service provider (IoC Container).</param>
        protected Action<Aggregate, Command> CompileAction(MethodInfo handleMethod, Type commandType, IServiceProvider serviceProvider)
        {
            var commandParameter = Expression.Parameter(typeof(Command), "command");
            var aggregateParameter = Expression.Parameter(typeof(Aggregate), "aggregate");
            var body = Expression.Call(Expression.Convert(aggregateParameter, handleMethod.ReflectedType), handleMethod, GetMethodArguments(handleMethod, commandParameter, serviceProvider));
            var expression = Expression.Lambda<Action<Aggregate, Command>>(body, new[] { aggregateParameter, commandParameter });

            return expression.Compile();
        }

        /// <summary>
        /// Get the set of method arguments to pass in to the underlying call expression.
        /// </summary>
        /// <param name="method">The method definition.</param>
        /// <param name="commandParameter">The command parameter reference.</param>
        /// <param name="serviceProvider">The underlying service provider (IoC Container).</param>
        private static IEnumerable<Expression> GetMethodArguments(MethodInfo method, ParameterExpression commandParameter, IServiceProvider serviceProvider)
        {
            var parameters = method.GetParameters();

            yield return Expression.TypeAs(commandParameter, parameters.First().ParameterType);

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
