using System;
using System.Runtime.Serialization;

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
    /// Represents a collection of events associated with a single unit of work.
    /// </summary>
    [DataContract, Serializable]
    public class Commit
    {
        /// <summary>
        /// The unique identifier associated with this commit.
        /// </summary>
        [DataMember(Name="c")]
        public Guid CommitId { get; protected set; }

        /// <summary>
        /// The stream identifier associated with this commit.
        /// </summary>
        [DataMember(Name = "s")]
        public Guid StreamId { get; protected set; }

        /// <summary>
        /// Gets the current revision of the event stream.
        /// </summary>
        [DataMember(Name = "r")]
        public Int32 Revision { get; protected set; }

        /// <summary>
        /// Gets the time when the commit was persisted to the database.
        /// </summary>
        [DataMember(Name = "t")]
        public DateTime Timestamp { get; protected set; }

        /// <summary>
        /// Gets the set of events associated with this commit.
        /// </summary>
        [DataMember(Name = "e")]
        public EventCollection Events { get; protected set; }

        /// <summary>
        /// Gets the set of commit headers associated with this commit.
        /// </summary>
        [DataMember(Name = "h")]
        public HeaderCollection Headers { get; protected set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Commit"/>.
        /// </summary>
        /// <param name="commitId">The unique commit id.</param>
        /// <param name="streamId">The event stream id.</param>
        /// <param name="revision">The event stream revision.</param>
        /// <param name="events">The optional set of events associated with this commit.</param>
        /// <param name="headers">The optional set of headers associated with this commit.</param>
        public Commit(Guid commitId, Guid streamId, Int32 revision, EventCollection events, HeaderCollection headers)
        {
            Verify.NotEqual(Guid.Empty, commitId, "commitId");
            Verify.NotEqual(Guid.Empty, streamId, "streamId");
            Verify.GreaterThan(0, revision, "revision");

            CommitId = commitId;
            StreamId = streamId;
            Revision = revision;
            Timestamp = SystemTime.Now;
            Events = events ?? EventCollection.Empty;
            Headers = headers ?? HeaderCollection.Empty;
        }
    }
}
