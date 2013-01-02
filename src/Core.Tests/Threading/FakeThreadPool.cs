using Spark.Infrastructure.Threading;
using System;
using System.Collections.Generic;
using System.Threading;

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

namespace Spark.Infrastructure.Tests.Threading
{
    public class FakeThreadPool : IQueueUserWorkItems
    {
        private readonly Queue<Action> queuedUserWorkItems = new Queue<Action>();

        public Queue<Action> UserWorkItems { get { return queuedUserWorkItems; } }

        public void QueueUserWorkItem(Action action)
        {
            queuedUserWorkItems.Enqueue(action);
        }

        public void QueueUserWorkItem<T>(Action<T> action, T state)
        {
            queuedUserWorkItems.Enqueue(() => action(state));
        }

        public void UnsafeQueueUserWorkItem(Action action)
        {
            queuedUserWorkItems.Enqueue(action);
        }

        public void UnsafeQueueUserWorkItem<T>(Action<T> action, T state)
        {
            queuedUserWorkItems.Enqueue(() => action(state));
        }

        public void RunNext()
        {
            RunNext(1);
        }

        public void RunNext(Int32 times)
        {
            Thread.Sleep(1);

            var action = queuedUserWorkItems.Dequeue();
            for (var i = 0; i < times; i++)
                action.Invoke();
        }
    }
}
