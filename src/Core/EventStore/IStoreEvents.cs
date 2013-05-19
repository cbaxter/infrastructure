using System;
using System.Collections.Generic;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Messaging;

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
    /// Data access contract for an event store.
    /// </summary>
    public interface IStoreEvents
    {
        /// <summary>
        /// Get the specified commit sequence range starting after <paramref name="skip"/> and returing <paramref name="take"/> commits.
        /// </summary>
        /// <param name="skip">The commit sequence lower bound (exclusive).</param>
        /// <param name="take">The number of commits to include in the result.</param>
        IReadOnlyList<Commit> GetRange(Int64 skip, Int64 take);

        /// <summary>
        /// Get all known stream identifiers.
        /// </summary>
        /// <remarks>This method is not safe to call on an active event store; only use when new streams are not being committed.</remarks>
        IEnumerable<Guid> GetStreams();

        /// <summary>
        /// Gets all commits for the specified <paramref name="streamId"/> with a version greater than or equal to <paramref name="minimumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="minimumVersion">The minimum stream version (inclusive).</param>
        IEnumerable<Commit> GetStream(Guid streamId, Int32 minimumVersion);

        /// <summary>
        /// Deletes the specified event stream for <paramref name="streamId"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        void DeleteStream(Guid streamId);

        /// <summary>
        /// Appends a new commit to the event store.
        /// </summary>
        /// <param name="commit">The commit to append to the event store.</param>
        void Save(Commit commit);

        /// <summary>
        /// Migrates the commit <paramref name="headers"/> and <paramref name="events"/> for the specified <paramref name="commitId"/>.
        /// </summary>
        /// <param name="commitId">The unique commit identifier.</param>
        /// <param name="headers">The new commit headers.</param>
        /// <param name="events">The new commit events.</param>
        void Migrate(Guid commitId, HeaderCollection headers, EventCollection events);

        /// <summary>
        /// Deletes all existing commits from the event store.
        /// </summary>
        void Purge();
    }

    /// <summary>
    /// Extension methods of <see cref="IStoreEvents"/>
    /// </summary>
    public static class EventStoreExtensions
    {
        /// <summary>
        /// Gets all commits from the event store.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Commit> GetAll(this IStoreEvents eventStore)
        {
            Verify.NotNull(eventStore, "eventStore");

            return new PagedResult<Commit>(Settings.Eventstore.PageSize, (lastResult, page) => eventStore.GetRange(lastResult == null ? 0L : lastResult.Sequence.GetValueOrDefault(), page.Take));
        }

        /// <summary>
        /// Gets all commits for the specified <paramref name="streamId"/>.
        /// </summary>
        /// <param name="eventStore">The event store.</param>
        /// <param name="streamId">The unique stream identifier.</param>
        public static IEnumerable<Commit> GetStream(this IStoreEvents eventStore, Guid streamId)
        {
            Verify.NotNull(eventStore, "eventStore");

            return eventStore.GetStream(streamId, 1);
        }
    }
}
