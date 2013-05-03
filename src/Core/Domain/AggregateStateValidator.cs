using System;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.EventStore;

namespace Spark.Infrastructure.Domain
{
    public sealed class AggregateStateValidator : PipelineHook
    {
        public override void PostGet(Aggregate aggregate)
        {
            Verify.NotNull(aggregate, "aggregate");

            aggregate.VerifyCheckSum();
        }

        public override void PreSave(Aggregate aggregate, CommandContext context)
        {
            aggregate.VerifyCheckSum();
        }

        public override void PostSave(Aggregate aggregate, Commit commit, Exception ex)
        {
            aggregate.UpdateCheckSum();
        }
    }
}
