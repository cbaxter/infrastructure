using System;

/* Copyright (c) 2014 Spark Software Ltd.
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

namespace Spark.Cqrs.Domain
{
    /// <summary>
    /// A base for strongly typed <see cref="Entity"/> unique identifiers.
    /// </summary>
    public abstract class EntityId : ValueObject<Guid>
    {
        /// <summary>
        /// Initialize a new instance of <see cref="EntityId"/>.
        /// </summary>
        protected EntityId()
            : base(GuidStrategy.NewGuid())
        { }

        /// <summary>
        /// Initialize an existing instance of <see cref="EntityId"/>.
        /// </summary>
        /// <param name="value">The underlying entity identifier.</param>
        protected EntityId(Guid value)
            : base(value)
        { }

        /// <summary>
        /// Attempt to parse the <see cref="String"/> <paramref name="value"/> in to the base value type to be wrapped by a value object instance.
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <param name="result">The parsed instance of the underlying value type.</param>
        protected override Boolean TryParse(String value, out Guid result)
        {
            return Guid.TryParse(value, out result);
        }
    }
}
