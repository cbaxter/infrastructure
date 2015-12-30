using System;
using System.Threading.Tasks;
using Spark.Cqrs.Eventing;
using Spark.Messaging;

namespace Spark.Example.Benchmarks
{
    /// <summary>
    /// An event processor decorator class used to track processed events.
    /// </summary>
    internal class EventProcessorWrapper : IProcessMessages<EventEnvelope>
    {
        private readonly IProcessMessages<EventEnvelope> processor;
        private readonly Statistics statistics;

        /// <summary>
        /// Initializes a new instance of <see cref="EventProcessorWrapper"/>.
        /// </summary>
        /// <param name="processor">The event processor to decorate.</param>
        /// <param name="statistics">The statistics class.</param>
        public EventProcessorWrapper(IProcessMessages<EventEnvelope> processor, Statistics statistics)
        {
            this.statistics = statistics;
            this.processor = processor;
        }

        /// <summary>
        /// Processes the specified message instance synchornously.
        /// </summary>
        /// <param name="message">The message to process.</param>
        public void Process(Message<EventEnvelope> message)
        {
            processor.Process(message);
            statistics.DecrementQueuedEvents();
        }

        /// <summary>
        /// Processes the specified message instance asynchornously.
        /// </summary>
        /// <param name="message">The message to process.</param>
        public Task ProcessAsync(Message<EventEnvelope> message)
        {
            var task = processor.ProcessAsync(message);

            task.ContinueWith(antecedent => statistics.DecrementQueuedEvents());

            return task;
        }
    }
}
