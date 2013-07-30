using System;
using Spark.Cqrs.Eventing;
using Spark.Messaging;

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

namespace Spark.EventStore
{
    /// <summary>
    /// Data access contract for an event store.
    /// </summary>
    public interface IStoreEvents : IRetrieveEvents
    {
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
        /// Mark the specified commit as being dispatched.
        /// </summary>
        /// <param name="id">The unique commit identifier that has been dispatched.</param>
        void MarkDispatched(Int64 id);

        /// <summary>
        /// Migrates the commit <paramref name="headers"/> and <paramref name="events"/> for the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique commit identifier.</param>
        /// <param name="headers">The new commit headers.</param>
        /// <param name="events">The new commit events.</param>
        void Migrate(Int64 id, HeaderCollection headers, EventCollection events);

        /// <summary>
        /// Deletes all existing commits from the event store.
        /// </summary>
        void Purge();
    }
}
