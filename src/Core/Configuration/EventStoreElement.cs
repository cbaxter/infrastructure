using System;
using System.Configuration;
using Spark.EventStore;

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

namespace Spark.Configuration
{
    /// <summary>
    /// <see cref="IStoreEvents"/> configuration settings.
    /// </summary>
    public interface IStoreEventSettings
    {
        /// <summary>
        /// Returns <value>true</value> if commits should be marked as dispatched asynchronously; otherwise <value>false</value> to synchronously mark commits as dispatched (default = <value>true</value>).
        /// </summary>
        Boolean Async { get; }

        /// <summary>
        /// The async buffer batch size used when marking dispatched commits in the underlying data store (default = <value>1000</value>).
        /// </summary>
        Int32 BatchSize { get; }

        /// <summary>
        /// Optimizes the <see cref="IStoreEvents"/> implementation by omitting duplicate commit detection (default = <value>true</value>).
        /// </summary>
        Boolean DetectDuplicateCommits { get; }

        /// <summary>
        /// The time to wait when the async undispatched commit buffer contains less than <see cref="BatchSize"/> commits before flushing the buffer (default = <value>00:00:00.100</value>).
        /// </summary>
        TimeSpan FlushInterval { get; }

        /// <summary>
        /// Returns <value>true</value> if commits should be marked as dispatched; otherwise <value>false</value>.
        /// </summary>
        Boolean MarkDispatched { get; }

        /// <summary>
        /// The number of commits to include in a single page of a paged result (default = <value>100</value>).
        /// </summary>
        Int64 PageSize { get; }
    }

    internal sealed class EventStoreElement : ConfigurationElement, IStoreEventSettings
    {
        [ConfigurationProperty("async", IsRequired = false, DefaultValue = true)]
        public Boolean Async { get { return (Boolean)base["async"]; } }

        [ConfigurationProperty("batchSize", IsRequired = false, DefaultValue = 1000)]
        public Int32 BatchSize { get { return (Int32)base["batchSize"]; } }

        [ConfigurationProperty("detectDuplicateCommits", IsRequired = false, DefaultValue = true)]
        public Boolean DetectDuplicateCommits { get { return (Boolean)base["detectDuplicateCommits"]; } }

        [ConfigurationProperty("flushInterval", IsRequired = false, DefaultValue = "00:00:00.100")]
        public TimeSpan FlushInterval { get { return (TimeSpan)base["flushInterval"]; } }

        [ConfigurationProperty("markDispatched", IsRequired = false, DefaultValue = true)]
        public Boolean MarkDispatched { get { return (Boolean)base["markDispatched"]; } }

        [ConfigurationProperty("pageSize", IsRequired = false, DefaultValue = 100L)]
        public Int64 PageSize { get { return (Int64)base["pageSize"]; } }
    }
}
