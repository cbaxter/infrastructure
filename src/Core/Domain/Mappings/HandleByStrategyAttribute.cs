using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Spark.Infrastructure.Commanding;
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
    /// Base attribute for indicating apply method mapping strategy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public abstract class HandleByStrategyAttribute : Attribute
    {
        public static readonly HandleByStrategyAttribute Default = new HandleByConventionAttribute();

        /// <summary>
        /// Gets the handle method collection associated with the given aggregate type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        internal HandleMethodCollection GetHandleMethods(Type aggregateType, IServiceProvider serviceProvider) //TODO: Pass in service provideR?
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
        protected abstract HandleMethodCollection MapHandleMethodsFor(Type aggregateType, IServiceProvider serviceProvider);

        /// <summary>
        /// Compiles a handle method in to an invokable action.
        /// </summary>
        /// <param name="applyMethod">The reflected handle method.</param>
        /// <param name="commandType">The command type handled.</param>
        protected Action<Aggregate, Command> CompileAction(MethodInfo handleMethod, Type commandType, IServiceProvider serviceProvider)
        {
            var commandParameter = Expression.Parameter(typeof(Command), "command");
            var aggregateParameter = Expression.Parameter(typeof(Aggregate), "aggregate");
            var body = Expression.Call(Expression.Convert(aggregateParameter, handleMethod.ReflectedType), handleMethod, GetMethodArguments(handleMethod, commandParameter, serviceProvider));
            var expression = Expression.Lambda<Action<Aggregate, Command>>(body, new[] { aggregateParameter, commandParameter });
            
            return expression.Compile();
        }

        private IEnumerable<Expression> GetMethodArguments(MethodInfo method, ParameterExpression commandParameter, IServiceProvider serviceProvider)
        {
            var parameters = method.GetParameters();

            yield return Expression.TypeAs(commandParameter, parameters.First().ParameterType);

            foreach (var parameter in parameters.Skip(1))
                yield return Expression.Constant(serviceProvider.GetService(parameter.ParameterType));
        }
    }
}
