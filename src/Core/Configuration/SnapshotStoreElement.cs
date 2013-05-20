using System;
using System.Configuration;
using Spark.Infrastructure.EventStore;

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

namespace Spark.Infrastructure.Configuration
{
    /// <summary>
    /// <see cref="IStoreSnapshots"/> configuration settings.
    /// </summary>
    public interface IStoreSnapshotSettings
    {
        /// <summary>
        /// Returns <value>true</value> if snapshots should be written asynchronously in the background; otherwise <value>false</value> to immediately save snapshot (default = <value>true</value>).
        /// </summary>
        Boolean Async { get; }

        /// <summary>
        /// The async buffer batch size used when writing out snapshots to the underlying data store (default = <value>100</value>).
        /// </summary>
        Int32 BatchSize { get; }

        /// <summary>
        /// The time to wait when the async snapshot buffer contains less than <see cref="BatchSize"/> snapshots before flushing the buffer (default = <value>00:00:00.100</value>).
        /// </summary>
        TimeSpan FlushInterval { get; }

        /// <summary>
        /// Returns <value>true</value> if an existing snapshot is to be replaced on save; otherwise <value>false</value> to insert a new snapshot on save (default = <value>true</value>).
        /// </summary>
        Boolean ReplaceExisting { get; }
    }

    internal sealed class SnapshotStoreElement : ConfigurationElement, IStoreSnapshotSettings
    {
        [ConfigurationProperty("async", IsRequired = false, DefaultValue = true)]
        public Boolean Async { get { return (Boolean)base["async"]; } }

        [ConfigurationProperty("batchSize", IsRequired = false, DefaultValue = 100)]
        public Int32 BatchSize { get { return (Int32)base["batchSize"]; } }

        [ConfigurationProperty("flushInterval", IsRequired = false, DefaultValue = "00:00:00.100")]
        public TimeSpan FlushInterval { get { return (TimeSpan)base["flushInterval"]; } }

        [ConfigurationProperty("replaceExisting", IsRequired = false, DefaultValue = true)]
        public Boolean ReplaceExisting { get { return (Boolean)base["replaceExisting"]; } }
    }
}
