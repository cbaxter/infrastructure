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
    /// Represents a point in time snapshot of an event stream.
    /// </summary>
    public sealed class Snapshot
    {
        /// <summary>
        /// The stream identifier associated with this commit.
        /// </summary>
        public Guid StreamId { get; private set; }

        /// <summary>
        /// Gets the current version of the event stream.
        /// </summary>
        public Int32 Version { get; private set; }

        /// <summary>
        /// Gets the set of events associated with this commit.
        /// </summary>
        public Object State { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Commit"/>.
        /// </summary>
        /// <param name="streamId">The stream id.</param>
        /// <param name="version">The stream state version.</param>
        /// <param name="state">The stream state.</param>
        public Snapshot(Guid streamId, Int32 version, Object state)
        { 
            Verify.NotEqual(Guid.Empty, streamId, "streamId");
            Verify.GreaterThan(0, version, "version");
            Verify.NotNull(state, "state");

            StreamId = streamId;
            Version = version;
            State = state;
        }
    }
}
