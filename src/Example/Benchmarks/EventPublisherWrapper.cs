using System.Collections.Generic;
using Spark.Cqrs.Eventing;
using Spark.Messaging;

namespace Spark.Example.Benchmarks
{
    /// <summary>
    /// An event publisher decorator class used to track queued events.
    /// </summary>
    internal class EventPublisherWrapper : IPublishEvents
    {
        private readonly IPublishEvents bus;
        private readonly Statistics statistics;

        /// <summary>
        /// Initializes a new instance of <see cref="EventPublisherWrapper"/>
        /// </summary>
        /// <param name="bus">The event publisher to decorate.</param>
        /// <param name="statistics">The statistics class.</param>
        public EventPublisherWrapper(IPublishEvents bus, Statistics statistics)
        {
            this.statistics = statistics;
            this.bus = bus;
        }

        /// <summary>
        /// Publishes the specified <paramref name="payload"/> on the underlying message bus.
        /// </summary>
        /// <param name="headers">The set of message headers associated with the event.</param>
        /// <param name="payload">The event payload to be published.</param>
        public void Publish(IEnumerable<Header> headers, EventEnvelope payload)
        {
            statistics.IncrementQueuedEvents();
            bus.Publish(headers, payload);
        }
    }
}
