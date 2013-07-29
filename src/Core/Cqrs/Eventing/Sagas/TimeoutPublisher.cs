using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Spark.Messaging;

namespace Spark.Cqrs.Eventing.Sagas
{
    public sealed class TimeoutPublisher : PipelineHook
    {
        private readonly SortedDictionary<DateTime, HashSet<SagaReference>> sortedSagaTimeouts = new SortedDictionary<DateTime, HashSet<SagaReference>>();
        private readonly Dictionary<SagaReference, SagaTimeout> scheduledSagaTimeouts = new Dictionary<SagaReference, SagaTimeout>();
        private readonly List<SagaReference> publishedReferences = new List<SagaReference>();
        private readonly Object syncLock = new Object();
        private readonly IPublishEvents eventPublisher;
        private readonly IStoreSagas sagaStore;
        private readonly Timer timer;
        private DateTime upperBound;
        private Boolean disposed;

        public TimeoutPublisher(IStoreSagas sagaStore, IPublishEvents eventPublisher)
        {
            Verify.NotNull(sagaStore, "sagaStore");
            Verify.NotNull(eventPublisher, "eventPublisher");

            this.sagaStore = sagaStore;
            this.eventPublisher = eventPublisher;
            this.timer = new Timer(PublishScheduledTimeouts, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100)); //TODO: Make configurable?
        }

        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            if (!disposing || disposed)
                return;

            disposed = true;
            timer.Dispose();
        }

        private void PublishScheduledTimeouts(Object state)
        {
            lock (syncLock)
            {
                var now = SystemTime.Now;

                if (upperBound < now)
                {
                    upperBound = now.AddMinutes(20); //TODO: Make configurable?
                    foreach (var sagaTimeout in sagaStore.GetScheduledTimeouts(upperBound))
                        ScheduleTimeout(sagaTimeout);
                }

                foreach (var item in sortedSagaTimeouts.Where(item => item.Key <= now))
                {
                    foreach (var sagaReference in item.Value)
                    {
                        SagaTimeout sagaTimeout;
                        if (!scheduledSagaTimeouts.TryGetValue(sagaReference, out sagaTimeout))
                            return;

                        var eventVersion = new EventVersion(sagaTimeout.Version, 1, 1);
                        var e = new Timeout(sagaTimeout.SagaType, sagaTimeout.Timeout);

                        eventPublisher.Publish(HeaderCollection.Empty, new EventEnvelope(GuidStrategy.NewGuid(), sagaReference.SagaId, eventVersion, e));
                        publishedReferences.Add(sagaReference);
                    }
                }

                foreach (var sagaReference in publishedReferences)
                    ClearTimeout(sagaReference);

                publishedReferences.Clear();
            }
        }

        public override void PostSave(Saga saga, SagaContext context, Exception error)
        {
            if (saga == null || error != null)
                return;

            if (saga.Timeout.HasValue)
            {
                if (saga.Completed)
                    return;

                ScheduleTimeout(new SagaTimeout(saga.CorrelationId, saga.GetType(), saga.Version, saga.Timeout.GetValueOrDefault()));
            }
            else
            {
                ClearTimeout(new SagaReference(saga.GetType(), saga.CorrelationId));
            }
        }

        private void ScheduleTimeout(SagaTimeout sagaTimeout)
        {
            var sagaReference = new SagaReference(sagaTimeout.SagaType, sagaTimeout.SagaId);
            var timeout = sagaTimeout.Timeout;

            lock (syncLock)
            {
                if (timeout >= upperBound)
                    return;

                HashSet<SagaReference> sagaReferences;
                if (!sortedSagaTimeouts.TryGetValue(timeout, out sagaReferences))
                    sortedSagaTimeouts.Add(timeout, sagaReferences = new HashSet<SagaReference>());

                sagaReferences.Add(sagaReference);
                scheduledSagaTimeouts[sagaReference] = sagaTimeout;
            }
        }

        private void ClearTimeout(SagaReference sagaReference)
        {
            lock (syncLock)
            {
                SagaTimeout sagaTimeout;
                if (!scheduledSagaTimeouts.TryGetValue(sagaReference, out sagaTimeout))
                    return;

                scheduledSagaTimeouts.Remove(sagaReference);

                HashSet<SagaReference> sagaReferences;
                if (!sortedSagaTimeouts.TryGetValue(sagaTimeout.Timeout, out sagaReferences))
                    return;

                sagaReferences.Remove(sagaReference);
                if (sagaReferences.Count == 0)
                    sortedSagaTimeouts.Remove(sagaTimeout.Timeout);
            }
        }
    }
}
