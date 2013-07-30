using System;

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

namespace Spark.Cqrs.Eventing
{
    /// <summary>
    /// Represents an <see cref="Event"/> handler method executor.
    /// </summary>
    public class EventHandler
    {
        private readonly Func<Object> eventHandlerFactory;
        private readonly Action<Object, Event> executor;
        private readonly Type handlerType;
        private readonly Type eventType;

        /// <summary>
        /// The event handler <see cref="Type"/> associated with this event handler executor.
        /// </summary>
        public Type HandlerType { get { return handlerType; } }

        /// <summary>
        /// The event <see cref="Type"/> associated with this event handler executor.
        /// </summary>
        public Type EventType { get { return eventType; } }

        /// <summary>
        /// The event handler executor.
        /// </summary>
        internal Action<Object, Event> Executor { get { return executor; } }

        /// <summary>
        /// Initializes a new instance of <see cref="EventHandler"/>.
        /// </summary>
        /// <param name="handlerType">The event handler type.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="executor">The event handler executor.</param>
        /// <param name="eventHandlerFactory">The event handler factory.</param>
        public EventHandler(Type handlerType, Type eventType, Action<Object, Event> executor, Func<Object> eventHandlerFactory)
        {
            Verify.NotNull(executor, "executor");
            Verify.NotNull(eventType, "eventType");
            Verify.NotNull(handlerType, "handlerType");
            Verify.NotNull(eventHandlerFactory, "eventHandlerFactory");
            Verify.TypeDerivesFrom(typeof(Event), eventType, "eventType");

            this.eventHandlerFactory = eventHandlerFactory;
            this.handlerType = handlerType;
            this.eventType = eventType;
            this.executor = executor;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EventHandler"/> using a delegate <paramref name="delegateHandler"/> instance.
        /// </summary>
        /// <param name="delegateHandler">The delegate event handler.</param>
        protected EventHandler(EventHandler delegateHandler)
        {
            Verify.NotNull(delegateHandler, "delegateHandler");

            this.eventHandlerFactory = delegateHandler.eventHandlerFactory;
            this.handlerType = delegateHandler.handlerType;
            this.eventType = delegateHandler.eventType;
            this.executor = delegateHandler.executor;
        }

        /// <summary>
        /// Invokes the underlying <see cref="Object"/> event handler method using the specified <see cref="context"/>.
        /// </summary>
        /// <param name="context">The current event context.</param>
        public virtual void Handle(EventContext context)
        {
            if (context == null)
                return;

            Executor.Invoke(eventHandlerFactory(), context.Event);
        }

        /// <summary>
        /// Returns the <see cref="EventHandler"/> description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} Event Handler ({1})", EventType, HandlerType);
        }
    }
}
