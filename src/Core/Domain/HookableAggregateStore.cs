using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Logging;

namespace Spark.Infrastructure.Domain
{
    public sealed class HookableAggregateStore : IStoreAggregates
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IStoreAggregates aggregateStore;
        private readonly PipelineHook[] postSaveHooks;
        private readonly PipelineHook[] preSaveHooks;
        private readonly PipelineHook[] postGetHooks;
        private readonly PipelineHook[] preGetHooks;

        public HookableAggregateStore(IStoreAggregates aggregateStore, IEnumerable<PipelineHook> pipelineHooks)
            : this(aggregateStore, pipelineHooks.DefaultIfEmpty().AsList())
        { }

        private HookableAggregateStore(IStoreAggregates aggregateStore, IList<PipelineHook> pipelineHooks)
        {
            Verify.NotNull(aggregateStore, "aggregateStore");

            this.aggregateStore = aggregateStore;
            this.preGetHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPreGet).ToArray();
            this.postGetHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPostGet).ToArray();
            this.preSaveHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPreSave).ToArray();
            this.postSaveHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPostSave).ToArray();
        }

        public Aggregate Get(Type aggregateType, Guid id)
        {
            Aggregate aggregate;

            InvokePreGetHooks(aggregateType, id);

            aggregate = aggregateStore.Get(aggregateType, id);

            InvokePostGetHooks(aggregate);

            return aggregate;
        }

        private void InvokePreGetHooks(Type aggregateType, Guid id)
        {
            foreach (var pipelineHook in preGetHooks)
            {
                Log.TraceFormat("Invoking pre-get pipeline hook: {0}", pipelineHook);
                pipelineHook.PreGet(aggregateType, id);
            }
        }

        private void InvokePostGetHooks(Aggregate aggregate)
        {
            foreach (var pipelineHook in postGetHooks)
            {
                Log.TraceFormat("Invoking post-get pipeline hook: {0}", pipelineHook);
                pipelineHook.PostGet(aggregate);
            }
        }

        public Commit Save(Aggregate aggregate, CommandContext context)
        {
            Exception error = null;
            Commit commit = null;

            InvokePreSaveHooks(aggregate, context);

            try
            {
                commit = aggregateStore.Save(aggregate, context);
            }
            catch (Exception ex)
            {
                error = ex;
                throw;
            }
            finally
            {
                InvokePostSaveHooks(aggregate, commit, error);
            }

            return commit;
        }

        private void InvokePreSaveHooks(Aggregate aggregate, CommandContext context)
        {
            foreach (var pipelineHook in preSaveHooks)
            {
                Log.TraceFormat("Invoking pre-save pipeline hook: {0}", pipelineHook);
                pipelineHook.PreSave(aggregate, context);
            }
        }

        private void InvokePostSaveHooks(Aggregate aggregate, Commit commit, Exception error)
        {
            foreach (var pipelineHook in postSaveHooks)
            {
                Log.TraceFormat("Invoking post-save pipeline hook: {0}", pipelineHook);
                pipelineHook.PostSave(aggregate, commit, error);
            }
        }
    }
}
