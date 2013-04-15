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
    internal sealed class AggregateRepositoryElement : ConfigurationElement
    {
        /// <summary>
        /// The maximum amount of time to spend trying to process a command (default 00:00:10).
        /// </summary>
        [ConfigurationProperty("retryTimeout", IsRequired = false, DefaultValue = "00:00:10")]
        public TimeSpan RetryTimeout { get { return (TimeSpan)base["retryTimeout"]; } }
    }
}
