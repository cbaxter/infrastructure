using Spark.Infrastructure.Threading;
using System;
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
    public sealed class FakeMonitor : ISynchronizeAccess
    {
        public Action BeforeWait { get; set; }
        public Action AfterPulse { get; set; }

        public void Pulse(Object obj)
        {
            Monitor.Pulse(obj);

            if (AfterPulse != null)
                AfterPulse.Invoke();
        }

        public void PulseAll(Object obj)
        {
            Monitor.PulseAll(obj);

            if (AfterPulse != null)
                AfterPulse.Invoke();
        }

        public void Wait(Object obj)
        {
            if (BeforeWait != null)
                BeforeWait.Invoke();

            Monitor.Wait(obj);
        }

        public Boolean Wait(Object obj, TimeSpan timeout)
        {
            if (BeforeWait != null)
                BeforeWait.Invoke();

            return Monitor.Wait(obj, timeout);
        }

        public Boolean Wait(Object obj, TimeSpan timeout, Boolean exitContext)
        {
            if (BeforeWait != null)
                BeforeWait.Invoke();

            return Monitor.Wait(obj, timeout, exitContext);
        }
    }
}
