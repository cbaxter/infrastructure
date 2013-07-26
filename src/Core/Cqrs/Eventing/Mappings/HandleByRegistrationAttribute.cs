using System;

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
    /// Indicates that event handler event handle methods are explicitly mapped by specified <see cref="HandleMethodMapping"/> type.
    /// </summary>
    /// <remarks>Intended for use in medium trust environments while maintaining non-public handle methods.</remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class HandleByRegistrationAttribute : HandleByStrategyAttribute
    {
        private readonly Type handleMethodMappingType;

        /// <summary>
        /// Initializes a new instance of <see cref="HandleByRegistrationAttribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="HandleMethodMapping"/> containing the explicit handle method mapping.</param>
        public HandleByRegistrationAttribute(Type type)
        {
            Verify.NotNull(type, "type");
            Verify.TypeDerivesFrom(typeof(HandleMethodMapping), type, "type");

            handleMethodMappingType = type;
        }

        /// <summary>
        /// Maps the handle methods on the specified type.
        /// </summary>
        /// <param name="handlerType">The event handler type on which to locate handle methods.</param>
        /// <param name="serviceProvider">The underlying service provider (IoC Container).</param>
        protected override HandleMethodCollection MapHandleMethodsFor(Type handlerType, IServiceProvider serviceProvider)
        {
            var mappingBuilder = (HandleMethodMapping)Activator.CreateInstance(handleMethodMappingType);
            var handleMethods = mappingBuilder.GetMappings(serviceProvider);

            return new HandleMethodCollection(handleMethods);
        }
    }
}
