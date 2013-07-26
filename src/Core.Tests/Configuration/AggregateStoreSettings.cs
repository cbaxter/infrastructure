using System;
using Spark.Configuration;

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

namespace Test.Spark.Configuration
{
    public sealed class AggregateStoreSettings : IStoreAggregateSettings
    {
        public TimeSpan CacheSlidingExpiration { get; set; }
        public TimeSpan SaveRetryTimeout { get; set; }
        public Int32 SnapshotInterval { get; set; }

        public AggregateStoreSettings()
        {
            CacheSlidingExpiration = TimeSpan.FromMinutes(1);
            SaveRetryTimeout = TimeSpan.FromMilliseconds(100);
            SnapshotInterval = 5;
        }
    }
}
