using System;
using Spark.Logging;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Spark.Cqrs.Eventing
{
    /// <summary>
    /// Represents an <see cref="Event"/> handler method executor.
    /// </summary>
    public class EventHandler
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
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
        internal EventHandler(Type handlerType, Type eventType, Action<Object, Event> executor, Func<Object> eventHandlerFactory)
        {
            Verify.NotNull(executor, nameof(executor));
            Verify.NotNull(eventType, nameof(eventType));
            Verify.NotNull(handlerType, nameof(handlerType));
            Verify.NotNull(eventHandlerFactory, nameof(eventHandlerFactory));
            Verify.TypeDerivesFrom(typeof(Event), eventType, nameof(eventType));

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
            Verify.NotNull(delegateHandler, nameof(delegateHandler));

            this.eventHandlerFactory = delegateHandler.eventHandlerFactory;
            this.handlerType = delegateHandler.handlerType;
            this.eventType = delegateHandler.eventType;
            this.executor = delegateHandler.executor;
        }

        /// <summary>
        /// Invokes the underlying <see cref="Object"/> event handler method using the specified <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The current event context.</param>
        public virtual void Handle(EventContext context)
        {
            Verify.NotNull(context, nameof(context));

            Log.Trace("{0} handling event {0}", handlerType, context.Event);

            Executor.Invoke(eventHandlerFactory(), context.Event);
        }

        /// <summary>
        /// Returns the <see cref="EventHandler"/> description for this instance.
        /// </summary>
        public override String ToString()
        {
            return $"{EventType} Event Handler ({HandlerType})";
        }
    }
}
