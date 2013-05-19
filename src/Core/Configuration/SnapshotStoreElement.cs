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
        /// Returns <value>true</value> if an existing snapshot is to be replaced on save; otherwise <value>false</value> to insert a new snapshot on save (default = <value>true</value>).
        /// </summary>
        Boolean ReplaceExisting { get; }
    }

    internal sealed class SnapshotStoreElement : ConfigurationElement, IStoreSnapshotSettings
    {
        [ConfigurationProperty("replaceExisting", IsRequired = false, DefaultValue = true)]
        public Boolean ReplaceExisting { get { return (Boolean)base["replaceExisting"]; } }
    }
}
