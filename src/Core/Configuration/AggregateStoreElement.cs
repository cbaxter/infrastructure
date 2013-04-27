using System;
using System.Configuration;
using Spark.Infrastructure.Commanding;

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
    /// <see cref="CommandProcessor"/> configuration settings.
    /// </summary>
    internal sealed class AggregateStoreElement : ConfigurationElement
    {
        /// <summary>
        /// The maximum amount of time an aggregate will remain cached if not accessed.
        /// </summary>
        [ConfigurationProperty("cacheSlidingExpiration", IsRequired = false, DefaultValue = "00:10:00")]
        public TimeSpan CacheSlidingExpiration { get { return (TimeSpan)base["cacheSlidingExpiration"]; } }

        /// <summary>
        /// The maximum amount of time to spend trying to save a commit (default 00:00:10).
        /// </summary>
        [ConfigurationProperty("saveRetryTimeout", IsRequired = false, DefaultValue = "00:00:10")]
        public TimeSpan SaveRetryTimeout { get { return (TimeSpan)base["saveRetryTimeout"]; } }

        /// <summary>
        /// The number of aggregate versions between snapshots (default 100).
        /// </summary>
        [ConfigurationProperty("snapshotInterval", IsRequired = false, DefaultValue = "100")]
        public Int32 SnapshotInterval { get { return (Int32)base["snapshotInterval"]; } }
    }
}
