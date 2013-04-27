using System;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.EventStore;

namespace Spark.Infrastructure.Domain
{
    public abstract class PipelineHook
    {
        private static readonly Type PipelineHookType = typeof(PipelineHook);
        internal Boolean ImplementsPostSave { get; private set; }
        internal Boolean ImplementsPreSave { get; private set; }
        internal Boolean ImplementsPostGet { get; private set; }
        internal Boolean ImplementsPreGet { get; private set; }

        protected PipelineHook()
        {
            var type = GetType();

            ImplementsPreGet = type.GetMethod("PreGet").DeclaringType != PipelineHookType;
            ImplementsPostGet = type.GetMethod("PostGet").DeclaringType != PipelineHookType;
            ImplementsPreSave = type.GetMethod("PreSave").DeclaringType != PipelineHookType;
            ImplementsPostSave = type.GetMethod("PostSave").DeclaringType != PipelineHookType;
        }

        public virtual void PreGet(Type aggregateType, Guid id)
        { }

        public virtual void PostGet(Aggregate aggregate)
        { }

        public virtual void PreSave(CommandContext context, Aggregate aggregate)
        { }

        public virtual void PostSave(CommandContext context, Commit commit)
        { }
    }
}
