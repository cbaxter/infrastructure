using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Resources;

namespace Spark.Cqrs.Eventing.Sagas
{
    public sealed class SagaMetadata
    {

        private readonly Type sagaType;
        private readonly HashSet<Type> initiatingEvents;
        private readonly Dictionary<Type, Func<Event, Guid>> resolvers;

        public SagaMetadata(Type sagaType, IEnumerable<Type> initiatingEvents, IEnumerable<KeyValuePair<Type, Func<Event, Guid>>> resolvers)
        {
            Verify.NotNull(sagaType, "sagaType");
            Verify.TypeDerivesFrom(typeof(Saga), sagaType, "sagaType");

            this.sagaType = sagaType;
            this.initiatingEvents = new HashSet<Type>(initiatingEvents ?? Enumerable.Empty<Type>());
            this.resolvers = (resolvers ?? Enumerable.Empty<KeyValuePair<Type, Func<Event, Guid>>>()).ToDictionary(item => item.Key, item => item.Value);
        }

        public Boolean CanStartWith(Type eventType)
        {
            Verify.NotNull(eventType, "eventType");

            return initiatingEvents.Contains(eventType);
        }

        public Boolean CanHandle(Type eventType)
        {
            Verify.NotNull(eventType, "eventType");

            return resolvers.ContainsKey(eventType);
        }

        public Guid GetCorrelationId<TEvent>(TEvent e)
            where TEvent : Event
        {
            Verify.NotNull(e, "e");

            Func<Event, Guid> resolver;
            if (resolvers.TryGetValue(e.GetType(), out resolver))
                return resolver(e);

            throw new InvalidOperationException(Exceptions.EventTypeAlreadyConfigured.FormatWith(sagaType, e.GetType()));
        }
    }
}
