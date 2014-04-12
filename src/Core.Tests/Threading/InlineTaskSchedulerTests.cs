using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spark.Threading;
using Xunit;

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

namespace Test.Spark.Threading
{
    namespace UsingInlineTaskScheduler
    {
        public class WhenQueuingTasks
        {
            [Fact]
            public void RunTaskImmediatelyOnCurrentThread()
            {
                Int32? managedThreadId = null;

                Task.Factory.StartNew(() => managedThreadId = Thread.CurrentThread.ManagedThreadId, CancellationToken.None, TaskCreationOptions.None, InlineTaskScheduler.Instance);

                Assert.True(managedThreadId.HasValue);
                Assert.Equal(Thread.CurrentThread.ManagedThreadId, managedThreadId.Value);
            }

            [Fact]
            public void MaximumConcurrencyLevelIsOne()
            {
                Assert.Equal(1, InlineTaskScheduler.Instance.MaximumConcurrencyLevel);
            }

            [Fact]
            public void ScheduledTasksAlwaysEmpty()
            {
                Assert.Equal(0, InlineTaskScheduler.Instance.ScheduledTasks.Count());
            }
        }

        public class WhenRunningTasksInline
        {
            [Fact]
            public void RunTaskImmediatelyOnCurrentThread()
            {
                Int32? managedThreadId = null;

                new Task(() => managedThreadId = Thread.CurrentThread.ManagedThreadId, CancellationToken.None, TaskCreationOptions.None).RunSynchronously(InlineTaskScheduler.Instance);

                Assert.True(managedThreadId.HasValue);
                Assert.Equal(Thread.CurrentThread.ManagedThreadId, managedThreadId.Value);
            }
        }
    }
}
