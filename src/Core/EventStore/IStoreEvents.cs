using System;
using System.Collections.Generic;

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
        /// Initializes a new event store.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets all commits from the event store.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Commit> GetAll();

        /// <summary>
        /// Gets all commits from the event store commited on or after <paramref name="startTime"/>.
        /// </summary>
        /// <param name="startTime">The timestamp of the first commit to be returned (inclusive).</param>
        /// <returns></returns>
        IEnumerable<Commit> GetFrom(DateTime startTime);

        /// <summary>
        /// Gets all commits for the specified <paramref name="streamId"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        IEnumerable<Commit> GetStream(Guid streamId);

        /// <summary>
        /// Gets all commits for the specified <paramref name="streamId"/> with a version greater than or equal to <paramref name="minimumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="minimumVersion">The minimum stream version (inclusive).</param>
        IEnumerable<Commit> GetStreamFrom(Guid streamId, Int32 minimumVersion);

        /// <summary>
        /// Adds a new commit to the event store.
        /// </summary>
        /// <param name="commit">The commit to append to the event store.</param>
        void SaveCommit(Commit commit);

        /// <summary>
        /// Migrates the commit <paramref name="headers"/> and <paramref name="events"/> for the specified <paramref name="commitId"/>.
        /// </summary>
        /// <param name="commitId">The unique commit identifier.</param>
        /// <param name="headers">The new commit headers.</param>
        /// <param name="events">The new commit events.</param>
        void Migrate(Guid commitId, HeaderCollection headers, EventCollection events);

        /// <summary>
        /// Deletes the specified event stream for <paramref name="streamId"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        void Purge(Guid streamId);

        /// <summary>
        /// Deletes all existing commits from the event store.
        /// </summary>
        void Purge();
    }
}
