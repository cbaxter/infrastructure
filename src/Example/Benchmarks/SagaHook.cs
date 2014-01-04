using System;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Data;

namespace Spark.Example.Benchmarks
{
    /// <summary>
    /// A pipeline hook to capture number of saga conflicts.
    /// </summary>
    internal sealed class SagaHook : PipelineHook
    {
        private readonly Statistics statistics;

        /// <summary>
        /// Initalizes a new instance of <see cref="SagaHook"/>.
        /// </summary>
        /// <param name="statistics">The statistics class.</param>
        public SagaHook(Statistics statistics)
        {
            this.statistics = statistics;
        }

        /// <summary>
        ///  When overriden, defines the custom behavior to be invoked after attempting to save the specified <paramref name="saga"/>.
        /// </summary>
        /// <param name="saga">The modified <see cref="Saga"/> instance if <paramref name="error"/> is <value>null</value>; otherwise the original <see cref="Saga"/> instance if <paramref name="error"/> is not <value>null</value>.</param>
        /// <param name="context">The current <see cref="SagaContext"/> assocaited with the pending saga modifications.</param>
        /// <param name="error">The <see cref="Exception"/> thrown if the save was unsuccessful; otherwise <value>null</value>.</param>
        public override void PostSave(Saga saga, SagaContext context, Exception error)
        {
            base.PostSave(saga, context, error);

            if (error is ConcurrencyException)
                statistics.IncrementConflictCount();
        }
    }
}
