using System;
using System.Security.Cryptography;
using System.Text;
using Spark.Configuration;
using Spark.EventStore;
using Spark.Logging;
using Spark.Resources;
using Spark.Threading;

namespace Spark.Eventing.Sagas
{
    public sealed class SagaEventHandler : EventHandler
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly TimeSpan retryTimeout;
        private readonly Guid sagaTypeId;

        public SagaEventHandler(EventHandler eventHandler)
            : this(eventHandler, Settings.EventProcessor)
        { }

        internal SagaEventHandler(EventHandler eventHandler, IProcessEventSettings settings)
            : base(eventHandler)
        {
            this.retryTimeout = settings.RetryTimeout;
        }


        private static Guid GetSagaTypeId(Type sagaType)
        {
            using (var hash = new MD5CryptoServiceProvider())
            {
                var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(sagaType.FullName));

                return new Guid(bytes);
            }
        }

        public static Guid GetSagaIdFrom(Event e)
        {
            return Guid.Empty;
        }

        public override void Handle(EventContext context)
        {
            var backoffContext = default(ExponentialBackoff);
            var sagaId = GetSagaIdFrom(context.Event);
            var done = false;

            do
            {
                try
                {
                    using (var sagaContext = new SagaContext(HandlerType, sagaId /*, context.Event*/))
                        UpdateSaga(sagaContext);

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

            using(Saga.AquireLock(context.SagaType, context.SagaId))
            {
                //Saga saga;
                //if (!sagaStore.TryGetSaga(sagaType, sagaId, out saga) && CanStartWith(context.Event))
                //    saga = (Saga)Activator.CreateInstance(sagaType);

                //if (saga != null)
                //{
                //    Log.DebugFormat("Handling event {0} on saga {1}-{2}", context.Event, sagaType, sagaId);

                //    Executor(saga, e);

                //    Log.Trace("Saving saga state");

                //    sagaStore.Save(saga, context);

                //    Log.Trace("Saga state saved");
                //}
                //else
                //{
                //    Log.Trace("Saga {0} not initiated by event {1}", sagaType, context.Event);
                //}
            }
        }
    }
}
