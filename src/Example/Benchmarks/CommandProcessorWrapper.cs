using System;
using System.Threading.Tasks;
using Spark.Cqrs.Commanding;
using Spark.Messaging;

namespace Spark.Example.Benchmarks
{
    /// <summary>
    /// A command processor decorator class used to track processed commands.
    /// </summary>
    internal class CommandProcessorWrapper : IProcessMessages<CommandEnvelope>
    {
        private readonly IProcessMessages<CommandEnvelope> processor;
        private readonly Statistics statistics;

        /// <summary>
        /// Initializes a new instance of <see cref="CommandProcessorWrapper"/>.
        /// </summary>
        /// <param name="processor">The command processor to decorate.</param>
        /// <param name="statistics">The statistics class.</param>
        public CommandProcessorWrapper(IProcessMessages<CommandEnvelope> processor, Statistics statistics)
        {
            this.statistics = statistics;
            this.processor = processor;
        }

        /// <summary>
        /// Processes the specified message instance synchornously.
        /// </summary>
        /// <param name="message">The message to process.</param>
        public void Process(Message<CommandEnvelope> message)
        {
            processor.Process(message);
            statistics.DecrementQueuedCommands();
        }

        /// <summary>
        /// Processes the specified message instance asynchornously.
        /// </summary>
        /// <param name="message">The message to process.</param>
        public Task ProcessAsync(Message<CommandEnvelope> message)
        {
            var task = processor.ProcessAsync(message);

            task.ContinueWith(antecedent => statistics.DecrementQueuedCommands());

            return task;
        }
    }
}
