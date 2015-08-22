using System;
using Spark.Cqrs.Domain;
using Spark.Data;
using Spark.EventStore;

namespace Spark.Example.Benchmarks
{
    /// <summary>
    /// A pipeline hook to capture number of commands processed.
    /// </summary>
    internal sealed class CommandHook : PipelineHook
    {
        private readonly Statistics statistics;

        /// <summary>
        /// Initalizes a new instance of <see cref="CommandHook"/>.
        /// </summary>
        /// <param name="statistics">The statistics class.</param>
        public CommandHook(Statistics statistics)
        {
            this.statistics = statistics;
        }

        /// <summary>
        ///  When overriden, defines the custom behavior to be invoked after attempting to save the specified <paramref name="aggregate"/>.
        /// </summary>
        /// <param name="aggregate">The modified <see cref="Aggregate"/> instance if <paramref name="commit"/> is not <value>null</value>; otherwise the original <see cref="Aggregate"/> instance if <paramref name="error"/> is not <value>null</value>.</param>
        /// <param name="commit">The <see cref="Commit"/> generated if the save was successful; otherwise <value>null</value>.</param>
        /// <param name="error">The <see cref="Exception"/> thrown if the save was unsuccessful; otherwise <value>null</value>.</param>
        public override void PostSave(Aggregate aggregate, Commit commit, Exception error)
        {
            base.PostSave(aggregate, commit, error);

            if (error is ConcurrencyException)
                statistics.IncrementConflictCount();
        }
    }
}
