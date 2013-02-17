using System;

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
    /// Indicates that aggregate event apply methods are explicitly mapped by specified <see cref="ApplyMethodMapping"/> type.
    /// </summary>
    /// <remarks>Indented for use in medium trust environments while maintaining non-public apply methods.</remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ApplyByRegistrationAttribute : ApplyByStrategyAttribute
    {
        private readonly Type applyMethodMappingType;

        /// <summary>
        /// Initializes a new instance of <see cref="ApplyByRegistrationAttribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="ApplyMethodMapping"/> containing the explicit apply method mapping.</param>
        public ApplyByRegistrationAttribute(Type type)
        {
            Verify.NotNull(type, "type");
            Verify.TypeDerivesFrom(typeof(ApplyMethodMapping), type, "type");

            applyMethodMappingType = type;
        }
        
        /// <summary>
        /// Maps the apply methods on the specified type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type on which to locate apply methods.</param>
        protected override ApplyMethodCollection MapApplyMethodsFor(Type aggregateType)
        {
            var mappingBuilder = (ApplyMethodMapping)Activator.CreateInstance(applyMethodMappingType);
            var applyMethods = mappingBuilder.GetMappings();

            return new ApplyMethodCollection(ApplyOptional, applyMethods);
        }
    }
}
