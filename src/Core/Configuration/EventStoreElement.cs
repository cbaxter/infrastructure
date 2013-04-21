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
    internal sealed class EventStoreElement : ConfigurationElement
    {
        /// <summary>
        /// Optimizes the <see cref="IStoreEvents"/> implementation for <see cref="IStoreEvents.GetStream"/> use only (i.e., no timestamp index created for <see cref="IStoreEvents.GetFrom"/>).
        /// </summary>
        [ConfigurationProperty("useGetStreamOnly", IsRequired = false, DefaultValue = false)]
        public Boolean UseGetStreamOnly { get { return (Boolean)base["useGetStreamOnly"]; } }

        /// <summary>
        /// Optimizes the <see cref="IStoreEvents"/> implementation by omitting the duplicate commit detection (i.e., no unique index on commitId).
        /// </summary>
        [ConfigurationProperty("detectDuplicateCommits", IsRequired = false, DefaultValue = true)]
        public Boolean DetectDuplicateCommits { get { return (Boolean)base["detectDuplicateCommits"]; } }
    }
}
