using System;
using System.Configuration;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Threading;

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
    /// <see cref="CommandReceiver"/> configuration settings.
    /// </summary>
    internal sealed class CommandReceiverElement : ConfigurationElement
    {
        /// <summary>
        /// The maximum concurrency level associated with the <see cref="CommandReceiver"/>'s <see cref="PartitionedTaskScheduler"/> (default 25).
        /// </summary>
        [ConfigurationProperty("maximumConcurrencyLevel", IsRequired = false, DefaultValue = 25)]
        public Int32 MaximumConcurrencyLevel { get { return (Int32)base["maximumConcurrencyLevel"]; } }

        /// <summary>
        /// The bounded capacity associated with the <see cref="CommandReceiver"/>'s <see cref="PartitionedTaskScheduler"/> (default 100).
        /// </summary>
        [ConfigurationProperty("boundedCapacity", IsRequired = false, DefaultValue = 100)]
        public Int32 BoundedCapacity { get { return (Int32)base["boundedCapacity"]; } }
    }
}
