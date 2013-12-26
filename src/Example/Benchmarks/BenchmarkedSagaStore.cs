using System;
using System.Collections.Generic;
using Spark.Cqrs.Eventing.Sagas;

namespace Spark.Example.Benchmarks
{
    /// <summary>
    /// A saga store decorator class to capture read/write operations.
    /// </summary>
    internal sealed class BenchmarkedSagaStore : IStoreSagas
    {
        private readonly Statistics statistics;
        private readonly IStoreSagas sagaStore;

        /// <summary>
        /// Initalizes a new isntance of <see cref="BenchmarkedSagaStore"/>.
        /// </summary>
        /// <param name="sagaStore">The saga store to decorate.</param>
        /// <param name="statistics">The statistics class.</param>
        public BenchmarkedSagaStore(IStoreSagas sagaStore, Statistics statistics)
        {
            this.sagaStore = sagaStore;
            this.statistics = statistics;
        }

        /// <summary>
        /// Creates a new saga instance identified by the specified <paramref name="type"/> and <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The type of saga to be retrieved.</param>
        /// <param name="id">The correlation id of the saga to be retrieved.</param>
        public Saga CreateSaga(Type type, Guid id)
        {
            var result = sagaStore.CreateSaga(type, id);

            statistics.IncrementQueryCount();

            return result;
        }

        /// <summary>
        /// Attempt to retrieve an existing saga instance identified by the specified <paramref name="type"/> and <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The type of saga to be retrieved.</param>
        /// <param name="id">The correlation id of the saga to be retrieved.</param>
        /// <param name="saga">The <see cref="Saga"/> instance if found; otherwise <value>null</value>.</param>
        public Boolean TryGetSaga(Type type, Guid id, out Saga saga)
        {
            var result = sagaStore.TryGetSaga(type, id, out saga);

            statistics.IncrementQueryCount();

            return result;
        }

        /// <summary>
        /// Get all scheduled saga timeouts before the specified maximum timeout.
        /// </summary>
        /// <param name="maximumTimeout">The exclusive timeout upper bound.</param>
        public IReadOnlyList<SagaTimeout> GetScheduledTimeouts(DateTime maximumTimeout)
        {
            var result = sagaStore.GetScheduledTimeouts(maximumTimeout);

            statistics.IncrementQueryCount();

            return result;
        }

        /// <summary>
        /// Save the specified <paramref name="context"/> changes for the given <paramref name="saga"/>.
        /// </summary>
        /// <param name="saga">The current saga version for which the context applies.</param>
        /// <param name="context">The saga context containing the saga changes to be applied.</param>
        public Saga Save(Saga saga, SagaContext context)
        {
            var result = sagaStore.Save(saga, context);

            if (saga.Version == 1)
            {
                statistics.IncrementInsertCount();
            }
            else
            {
                if (saga.Completed)
                {
                    statistics.IncrementDeleteCount();
                }
                else
                {
                    statistics.IncrementUpdateCount();
                }
            }

            return result;
        }

        /// <summary>
        /// Deletes all existing sagas from the saga store.
        /// </summary>
        public void Purge()
        {
            sagaStore.Purge();

            statistics.IncrementDeleteCount();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            sagaStore.Dispose();
        }
    }
}
