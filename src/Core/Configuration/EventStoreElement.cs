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
    /// <see cref="IStoreEvents"/> configuration settings.
    /// </summary>
    public interface IStoreEventSettings
    {
        /// <summary>
        /// The number of commits to include in a single page of a paged result (default = <value>100</value>).
        /// </summary>
        Int64 PageSize { get; }

        /// <summary>
        /// Optimizes the <see cref="IStoreEvents"/> implementation by omitting duplicate commit detection (default = <value>true</value>).
        /// </summary>
        Boolean DetectDuplicateCommits { get; }
    }

    internal sealed class EventStoreElement : ConfigurationElement, IStoreEventSettings
    {
        [ConfigurationProperty("pageSize", IsRequired = false, DefaultValue = 100L)]
        public Int64 PageSize { get { return (Int64)base["pageSize"]; } }

        [ConfigurationProperty("detectDuplicateCommits", IsRequired = false, DefaultValue = true)]
        public Boolean DetectDuplicateCommits { get { return (Boolean)base["detectDuplicateCommits"]; } }
    }
}
