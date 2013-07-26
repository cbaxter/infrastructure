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

namespace Spark.EventStore
{
    /// <summary>
    /// Extension methods of <see cref="IRetrieveSnapshots"/> and  <see cref="IStoreSnapshots"/>.
    /// </summary>
    public static class SnapshotStoreExtensions
    {
        /// <summary>
        /// Gets the last snapshot for the specified <paramref name="streamId"/>.
        /// </summary>
        /// <param name="snapshotStore">The snapshot store.</param>
        /// <param name="streamId">The unique stream identifier.</param>
        public static Snapshot GetLastSnapshot(this IRetrieveSnapshots snapshotStore, Guid streamId)
        {
            Verify.NotNull(snapshotStore, "snapshotStore");

            return snapshotStore.GetSnapshot(streamId, Int32.MaxValue);
        }
    }
}
