using System;
using Spark.Configuration;
using Spark.Data;
using Spark.Logging;
using Spark.Resources;
using Spark.Threading;

namespace Spark.Cqrs.Eventing.Sagas
{
    public sealed class SagaEventHandler : EventHandler
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly SagaMetadata sagaMetadata;
        private readonly IStoreSagas sagaStore;
        private readonly TimeSpan retryTimeout;

        public SagaEventHandler(EventHandler eventHandler, SagaMetadata sagaMetadata, IStoreSagas sagaStore)
            : this(eventHandler, sagaMetadata, sagaStore, Settings.EventProcessor)
        { }

        internal SagaEventHandler(EventHandler eventHandler, SagaMetadata sagaMetadata, IStoreSagas sagaStore, IProcessEventSettings settings)
            : base(eventHandler)
        {
            this.sagaStore = sagaStore;
            this.sagaMetadata = sagaMetadata;
            this.retryTimeout = settings.RetryTimeout;
        }

        public override void Handle(EventContext context)
        {
            var sagaId = sagaMetadata.GetCorrelationId(context.Event);
            var backoffContext = default(ExponentialBackoff);
            var done = false;

            do
            {
                try
                {
                    using (var sagaContext = new SagaContext(HandlerType, sagaId, context.Event))
                    {
                        UpdateSaga(sagaContext);

                        //TODO: Publish commands...
                    }

                    done = true;
                }
                catch (ConcurrencyException ex)
                {
                    if (backoffContext == null)
                        backoffContext = new ExponentialBackoff(retryTimeout);

                    if (!backoffContext.CanRetry)
                        throw new TimeoutException(Exceptions.UnresolvedConcurrencyConflict.FormatWith(context), ex);

                    Log.WarnFormat("Concurrency conflict: {0}", context);
                    backoffContext.WaitUntilRetry();
                }
            } while (!done);
        }

        private void UpdateSaga(SagaContext context)
        {
            var sagaType = context.SagaType;
            var sagaId = context.SagaId;
            var e = context.Event;

            using (Saga.AquireLock(sagaType, sagaId))
            {
                Saga saga;
                if (!sagaStore.TryGetSaga(sagaType, sagaId, out saga) && sagaMetadata.CanStartWith(e.GetType()))
                    saga = sagaStore.CreateSaga(sagaType, sagaId);

                if (saga != null)
                {
                    Log.DebugFormat("Handling event {0} on saga {1}-{2}", context.Event, sagaType, sagaId);

                    Executor(saga, e);

                    Log.Trace("Saving saga state");

                    sagaStore.Save(saga, context);

                    Log.Trace("Saga state saved");
                }
                else
                {
                    Log.TraceFormat("Saga {0} is not initiated by event {1}", sagaType, context.Event);
                }
            }
        }
    }
}
