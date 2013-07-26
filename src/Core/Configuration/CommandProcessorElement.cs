using System;
using System.Configuration;
using Spark.Commanding;
using Spark.Threading;

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
    /// <see cref="CommandProcessor"/> configuration settings.
    /// </summary>
    public interface IProcessCommandSettings
    {
        /// <summary>
        /// The bounded capacity associated with the <see cref="CommandProcessor"/>'s <see cref="PartitionedTaskScheduler"/> (default 1000).
        /// </summary>
        Int32 BoundedCapacity { get; }
        
        /// <summary>
        /// The maximum concurrency level associated with the <see cref="CommandProcessor"/>'s <see cref="PartitionedTaskScheduler"/> (default 47).
        /// </summary>
        Int32 MaximumConcurrencyLevel { get; }

        /// <summary>
        /// The maximum amount of time to spend trying to process a command (default 00:00:10).
        /// </summary>
        TimeSpan RetryTimeout { get; }
    }

    internal sealed class CommandProcessorElement : ConfigurationElement, IProcessCommandSettings
    {
        [ConfigurationProperty("boundedCapacity", IsRequired = false, DefaultValue = 1000)]
        public Int32 BoundedCapacity { get { return (Int32)base["boundedCapacity"]; } }

        [ConfigurationProperty("maximumConcurrencyLevel", IsRequired = false, DefaultValue = 47)]
        public Int32 MaximumConcurrencyLevel { get { return (Int32)base["maximumConcurrencyLevel"]; } }

        [ConfigurationProperty("retryTimeout", IsRequired = false, DefaultValue = "00:00:10")]
        public TimeSpan RetryTimeout { get { return (TimeSpan)base["retryTimeout"]; } }
    }
}
