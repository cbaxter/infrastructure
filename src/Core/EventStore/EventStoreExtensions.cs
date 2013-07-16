using System;
using System.Collections.Generic;
using Spark.Infrastructure.Configuration;

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

namespace Spark.Infrastructure.EventStore
{
    /// <summary>
    /// Extension methods of <see cref="IRetrieveEvents"/> and  <see cref="IStoreEvents"/>.
    /// </summary>
    public static class EventStoreExtensions
    {
        /// <summary>
        /// Gets all commits from the event store.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Commit> GetAll(this IRetrieveEvents eventStore)
        {
            Verify.NotNull(eventStore, "eventStore");

            return new PagedResult<Commit>(Settings.Eventstore.PageSize, (lastResult, page) => eventStore.GetRange(lastResult == null ? 0L : lastResult.Id.GetValueOrDefault(), page.Take));
        }

        /// <summary>
        /// Gets all commits for the specified <paramref name="streamId"/>.
        /// </summary>
        /// <param name="eventStore">The event store.</param>
        /// <param name="streamId">The unique stream identifier.</param>
        public static IEnumerable<Commit> GetStream(this IRetrieveEvents eventStore, Guid streamId)
        {
            Verify.NotNull(eventStore, "eventStore");

            return eventStore.GetStream(streamId, 1);
        }
    }
}
