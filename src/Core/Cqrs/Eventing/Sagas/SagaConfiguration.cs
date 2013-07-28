using System;
using System.Collections.Generic;
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
    /// Collects saga configuration metadata for a given saga instance.
    /// </summary>
    public sealed class SagaConfiguration
    {
        private readonly Dictionary<Type, Func<Event, Guid>> handledEvents = new Dictionary<Type, Func<Event, Guid>>();
        private readonly HashSet<Type> initiatingEvents = new HashSet<Type>();
        private readonly Type sagaType;

        /// <summary>
        /// Initializes a new instance of <see cref="SagaConfiguration"/> for the specified <paramref name="sagaType"/>.
        /// </summary>
        /// <param name="sagaType">The saga type associated with this configuration instance.</param>
        internal SagaConfiguration(Type sagaType)
        {
            Verify.NotNull(sagaType, "sagaType");
            Verify.TypeDerivesFrom(typeof(Saga), sagaType, "sagaType");

            this.sagaType = sagaType;
        }

        /// <summary>
        /// Registers a correlation ID resolver for <typeparamref name="TEvent"/> (<typeparamref name="TEvent"/> always handled)
        /// </summary>
        /// <typeparam name="TEvent">The type of <see cref="Event"/>.</typeparam>
        /// <param name="resolver">The correlation ID resolver function for <typeparamref name="TEvent"/>.</param>
        public void CanStartWith<TEvent>(Func<TEvent, Guid> resolver)
            where TEvent : Event
        {
            Verify.NotNull(resolver, "resolver");

            CanHandle(resolver);

            initiatingEvents.Add(typeof(TEvent));
        }

        /// <summary>
        /// Registers a correlation ID resolver for <typeparamref name="TEvent"/> (<typeparamref name="TEvent"/> handled if saga instance already exists)
        /// </summary>
        /// <typeparam name="TEvent">The type of <see cref="Event"/>.</typeparam>
        /// <param name="resolver">The correlation ID resolver function for <typeparamref name="TEvent"/>.</param>
        public void CanHandle<TEvent>(Func<TEvent, Guid> resolver)
            where TEvent : Event
        {
            Verify.NotNull(resolver, "resolver");

            if (handledEvents.ContainsKey(typeof(TEvent)))
                throw new ArgumentException(Exceptions.EventTypeAlreadyConfigured.FormatWith(sagaType, typeof(TEvent)));

            handledEvents.Add(typeof(TEvent), e => resolver((TEvent)e));
        }

        /// <summary>
        /// Get the configured <see cref="SagaMetadata"/> based on the current <see cref="SagaConfiguration"/> state.
        /// </summary>
        internal SagaMetadata GetMetadata()
        {
            return new SagaMetadata(sagaType, initiatingEvents, handledEvents);
        }
    }
}
