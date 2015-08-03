using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Resources;

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
namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// Saga metadata identifying handled and initiating events.
    /// </summary>
    public sealed class SagaMetadata
    {
        private readonly Type sagaType;
        private readonly HashSet<Type> initiatingEvents;
        private readonly Dictionary<Type, Func<Event, Guid>> resolvers;

        /// <summary>
        /// Initializes a new instance of <see cref="SagaMetadata"/> with the specified set of <paramref name="initiatingEvents"/> and event correlation ID <paramref name="resolvers"/>.
        /// </summary>
        /// <param name="sagaType">The saga type associated with this saga metadata.</param>
        /// <param name="initiatingEvents">The set of intiating event types associated with this saga metadata.</param>
        /// <param name="resolvers">The set of handled event resolvers used to lookup the saga correlation ID for a given event type.</param>
        public SagaMetadata(Type sagaType, IEnumerable<Type> initiatingEvents, IEnumerable<KeyValuePair<Type, Func<Event, Guid>>> resolvers)
        {
            Verify.NotNull(sagaType, "sagaType");
            Verify.TypeDerivesFrom(typeof(Saga), sagaType, "sagaType");

            this.sagaType = sagaType;
            this.initiatingEvents = new HashSet<Type>(initiatingEvents ?? Enumerable.Empty<Type>());
            this.resolvers = (resolvers ?? Enumerable.Empty<KeyValuePair<Type, Func<Event, Guid>>>()).ToDictionary(item => item.Key, item => item.Value);
        }

        /// <summary>
        /// Returns <value>true</value> if the associated saga type may be started when the specified <paramref name="eventType"/> is handled; otherwise <value>false</value>.
        /// </summary>
        /// <param name="eventType">The event type to test if can initiate a new saga instance.</param>
        public Boolean CanStartWith(Type eventType)
        {
            Verify.NotNull(eventType, "eventType");

            return initiatingEvents.Contains(eventType);
        }

        /// <summary>
        /// Returns <value>true</value> if the associated saga type may handle the specified <paramref name="eventType"/>; otherwise <value>false</value>.
        /// </summary>
        /// <param name="eventType">The event type to test if can initiate a new saga instance.</param>
        public Boolean CanHandle(Type eventType)
        {
            Verify.NotNull(eventType, "eventType");

            return resolvers.ContainsKey(eventType);
        }

        /// <summary>
        /// Gets the saga correlation ID based on the given event <paramref name="e"/>.
        /// </summary>
        /// <typeparam name="TEvent">The type of event for which the correlation ID is to be resolved.</typeparam>
        /// <param name="e">The event instance from which the correlation ID is to be resolved.</param>
        public Guid GetCorrelationId<TEvent>(TEvent e)
            where TEvent : Event
        {
            Verify.NotNull(e, "e");

            Func<Event, Guid> resolver;
            if (resolvers.TryGetValue(e.GetType(), out resolver))
                return resolver(e);

            throw new InvalidOperationException(Exceptions.EventTypeNotConfigured.FormatWith(sagaType, e.GetType()));
        }
    }
}
