using System.Configuration;

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
    internal static class Settings
    {
        /// <summary>
        /// Gets or sets the default <see cref="SparkConfigurationSection"/>.
        /// </summary>
        public static SparkConfigurationSection Default { get; set; }

        /// <summary>
        /// Initializes <see cref="Default"/> to the currently configured <see cref="SparkConfigurationSection"/>.
        /// </summary>
        static Settings()
        {
           Default = (SparkConfigurationSection)ConfigurationManager.GetSection("spark.infrastructure") ?? new SparkConfigurationSection();
        }

        /// <summary>
        /// The <see cref="CommandProcessor"/> configuration settings.
        /// </summary>
        public static CommandProcessorElement CommandProcessor
        {
            get { return Default.CommandProcessor; }
        }

        /// <summary>
        /// The <see cref="CommandReceiver"/> configuration settings.
        /// </summary>
        public static CommandReceiverElement CommandReceiver
        {
            get { return Default.CommandReceiver; }
        }
    }
}
