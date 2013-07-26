using System.Collections.Generic;
using Spark.Eventing;
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

namespace Spark.Tests.Eventing
{
    public static class UsingEventCollection
    {
        public class WhenReferencingEmptyEvents
        {
            [Fact]
            public void AlwaysReturnSameInstance()
            {
                Assert.Same(EventCollection.Empty, EventCollection.Empty);
            }
        }

        public class WhenCreatingEventCollection
        {
            [Fact]
            public void CanMutateUnderlyingCollection()
            {
                var list = new List<Event>();
                var events = new EventCollection(list);

                list.Add(new FakeEvent());

                Assert.Equal(1, events.Count);
            }

            private sealed class FakeEvent : Event
            { }
        }
    }
}
