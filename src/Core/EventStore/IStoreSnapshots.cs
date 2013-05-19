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

namespace Spark.Infrastructure.EventStore
{
    /// <summary>
    /// Data access contract for a snapshot store.
    /// </summary>
    public interface IStoreSnapshots
    {
        /// <summary>
        /// Gets the most recent snapshot for the specified <paramref name="streamId"/> and <paramref name="maximumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="maximumVersion">The maximum snapshot version.</param>
        Snapshot GetSnapshot(Guid streamId, Int32 maximumVersion);

        /// <summary>
        /// Adds a new snapshot to the snapshot store, keeping all existing snapshots.
        /// </summary>
        /// <param name="snapshot">The snapshot to append to the snapshot store.</param>
        void SaveSnapshot(Snapshot snapshot);

        /// <summary>
        /// Replaces any existing snapshot with the specified <paramref name="snapshot"/>.
        /// </summary>
        /// <param name="snapshot">The snapshot to replace any existing snapshot.</param>
        void ReplaceSnapshot(Snapshot snapshot);

        /// <summary>
        /// Deletes all existing snapshots from the snapshot store.
        /// </summary>
        void Purge();
    }
    
    /// <summary>
    /// Extension methods of <see cref="IStoreSnapshots"/>
    /// </summary>
    public static class SnapshotStoreExtensions
    {
        /// <summary>
        /// Gets the last snapshot for the specified <paramref name="streamId"/>.
        /// </summary>
        /// <param name="snapshotStore">The snapshot store.</param>
        /// <param name="streamId">The unique stream identifier.</param>
        public static Snapshot GetLastSnapshot(this IStoreSnapshots snapshotStore, Guid streamId)
        {
            Verify.NotNull(snapshotStore, "snapshotStore");

            return snapshotStore.GetSnapshot(streamId, Int32.MaxValue);
        }
    }
}
