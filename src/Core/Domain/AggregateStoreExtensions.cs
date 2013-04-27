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

namespace Spark.Infrastructure.Domain
{
    /// <summary>
    /// Extension methods of <see cref="IRetrieveAggregates"/> and <see cref="IStoreAggregates"/>.
    /// </summary>
    public static class AggregateStoreExtensions
    {
        /// <summary>
        /// Retrieve the aggregate of the specified <typeparamref name="TAggregate"/> type and aggregate <paramref name="id"/>.
        /// </summary>
        /// <typeparam name="TAggregate">The type of aggregate to retrieve.</typeparam>
        /// <param name="aggregateRepository">The aggregate repository from which the aggregate is to be retrieved.</param>
        /// <param name="id">The unique aggregate id.</param>
        public static TAggregate Get<TAggregate>(this IRetrieveAggregates aggregateRepository, Guid id)
            where TAggregate : Aggregate
        {
            Verify.NotNull(aggregateRepository, "aggregateRepository");

            return (TAggregate)aggregateRepository.Get(typeof(TAggregate), id);
        }
    }
}
