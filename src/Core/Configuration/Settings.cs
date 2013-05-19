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
    /// Static settings class to access `spark.infrastructure` configuration section.
    /// </summary>
    public static class Settings
    {
        private static readonly ISettings configuration;

        /// <summary>
        /// Gets the default application <see cref="ISettings"/>.
        /// </summary>
        public static ISettings Default { get { return configuration; } }

        /// <summary>
        /// Initializes <see cref="configuration"/> to the currently configured <see cref="SparkConfigurationSection"/>.
        /// </summary>
        static Settings()
        {
            configuration = (SparkConfigurationSection)ConfigurationManager.GetSection("spark.infrastructure") ?? new SparkConfigurationSection();
        }

        /// <summary>
        /// The <see cref="CommandProcessor"/> configuration settings.
        /// </summary>
        public static IStoreAggregateSettings AggregateStore
        {
            get { return configuration.AggregateStore; }
        }

        /// <summary>
        /// The <see cref="CommandProcessor"/> configuration settings.
        /// </summary>
        public static IProcessCommandSettings CommandProcessor
        {
            get { return configuration.CommandProcessor; }
        }

        /// <summary>
        /// The <see cref="CommandReceiver"/> configuration settings.
        /// </summary>
        public static IReceiveCommandSettings CommandReceiver
        {
            get { return configuration.CommandReceiver; }
        }

        /// <summary>
        /// The <see cref="IStoreEvents"/> configuration settings.
        /// </summary>
        public static IStoreEventSettings Eventstore
        {
            get { return configuration.EventStore; }
        }

        /// <summary>
        /// The <see cref="IStoreSnapshots"/> configuration settings.
        /// </summary>
        public static IStoreSnapshotSettings SnapshotStore
        {
            get { return configuration.SnapshotStore; }
        }
    }
}
