using System;
using System.Collections.ObjectModel;

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
    /// <summary>
    /// A collection of <see cref="Entity"/> objects uniquely identified by <see cref="Entity.Id"/>.
    /// </summary>
    public sealed class EntityCollection<TEntity> : KeyedCollection<Guid, TEntity>
        where TEntity : Entity
    {
        /// <summary>
        /// Extracts the unique <see cref="Entity"/> id from the specified <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The <see cref="Entity"/> for which the key is to be extracted.</param>
        protected override Guid GetKeyForItem(TEntity item)
        {
            return item == null ? Guid.Empty : item.Id;
        }
    }
}
