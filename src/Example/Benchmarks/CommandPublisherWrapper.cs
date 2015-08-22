using System.Collections.Generic;
using Spark.Cqrs.Commanding;
using Spark.Messaging;

namespace Spark.Example.Benchmarks
{
    /// <summary>
    /// A command publisher decorator class used to track queued commands.
    /// </summary>
    internal class CommandPublisherWrapper : IPublishCommands
    {
        private readonly IPublishCommands bus;
        private readonly Statistics statistics;

        /// <summary>
        /// Initializes a new instance of <see cref="CommandPublisherWrapper"/>
        /// </summary>
        /// <param name="bus">The command publisher to decorate.</param>
        /// <param name="statistics">The statistics class.</param>
        public CommandPublisherWrapper(IPublishCommands bus, Statistics statistics)
        {
            this.statistics = statistics;
            this.bus = bus;
        }

        /// <summary>
        /// Publishes the specified <paramref name="payload"/> on the underlying message bus.
        /// </summary>
        /// <param name="headers">The set of message headers associated with the command.</param>
        /// <param name="payload">The command payload to be published.</param>
        public void Publish(IEnumerable<Header> headers, CommandEnvelope payload)
        {
            statistics.IncrementQueuedCommands();
            bus.Publish(headers, payload);
        }
    }
}
