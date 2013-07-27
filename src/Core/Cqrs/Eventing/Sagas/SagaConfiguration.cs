using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spark.Resources;

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

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// Collects saga metadata for a given saga instance.
    /// </summary>
    public sealed class SagaConfiguration
    {
        private readonly Dictionary<Type, Func<Event, Guid>> handledEvents = new Dictionary<Type, Func<Event, Guid>>();
        private readonly HashSet<Type> initiatingEvents = new HashSet<Type>();
        private readonly Type sagaType;

        internal SagaConfiguration(Type sagaType)
        {
            Verify.NotNull(sagaType, "sagaType");

            this.sagaType = sagaType;
        }

        public void CanStartWith<TEvent>(Func<TEvent, Guid> resolver)
            where TEvent : Event
        {
            Verify.NotNull(resolver, "resolver");

            CanHandle(resolver);

            // NOTE: Using Dictionary<Type, Boolean> over HashSet<Type> as there is no IReadOnlySet<T> (only ISet<T>).
            initiatingEvents.Add(typeof(TEvent));
        }

        public void CanHandle<TEvent>(Func<TEvent, Guid> resolver)
            where TEvent : Event
        {
            Verify.NotNull(resolver, "resolver");

            if (handledEvents.ContainsKey(typeof(TEvent)))
                throw new ArgumentException(Exceptions.EventTypeAlreadyConfigured.FormatWith(sagaType, typeof(TEvent)));

            handledEvents.Add(typeof(TEvent), e => resolver((TEvent)e));
        }

        internal SagaMetadata GetMetadata()
        {
            return new SagaMetadata(sagaType, initiatingEvents, handledEvents);
        }
    }
}
