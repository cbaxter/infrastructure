using System;
using System.Configuration;
using Spark.Cqrs.Eventing.Sagas;

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
    /// <see cref="IStoreSagas"/> configuration settings.
    /// </summary>
    internal interface IStoreSagaSettings
    {
        /// <summary>
        /// The maximum amount of time a saga will remain cached if not accessed (default = <value>00:10:00</value>).
        /// </summary>
        TimeSpan CacheSlidingExpiration { get; }

        /// <summary>
        /// The saga timeout cache duration (default = <value>00:15:00</value>).
        /// </summary>
        TimeSpan TimeoutCacheDuration { get; }
    }

    internal sealed class SagaStoreElement : ConfigurationElement, IStoreSagaSettings
    {
        [ConfigurationProperty("cacheSlidingExpiration", IsRequired = false, DefaultValue = "00:10:00")]
        public TimeSpan CacheSlidingExpiration { get { return (TimeSpan)base["cacheSlidingExpiration"]; } }

        [ConfigurationProperty("timeoutCacheDuration", IsRequired = false, DefaultValue = "00:15:00")]
        public TimeSpan TimeoutCacheDuration { get { return (TimeSpan)base["timeoutCacheDuration"]; } }
    }
}
