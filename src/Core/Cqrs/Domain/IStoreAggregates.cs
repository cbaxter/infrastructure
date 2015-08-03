using System;
using Spark.Cqrs.Commanding;
using Spark.Resources;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Spark.Cqrs.Domain
{
    /// <summary>
    /// Saves aggregate changes to the underlying event store.
    /// </summary>
    public interface IStoreAggregates : IRetrieveAggregates, IDisposable
    {
        /// <summary>
        /// Save the specified <paramref name="context"/> changes for the given aggregate.
        /// </summary>
        /// <param name="aggregate">The current aggregate version for which the context applies.</param>
        /// <param name="context">The command context containing the aggregate changes to be applied.</param>
        SaveResult Save(Aggregate aggregate, CommandContext context);
    }

    /// <summary>
    /// Extension methods of <see cref="IStoreAggregates"/>.
    /// </summary>
    public static class StoreAggregateExtensions
    {
        /// <summary>
        /// Creates an aggregate of type <typeparamref name="TAggregate"/> identified by <paramref name="id"/> if does not already exist; otherwise throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <typeparam name="TAggregate">The type of aggregate to retrieve.</typeparam>
        /// <param name="aggregateStore">The aggregate repository from which the aggregate is to be retrieved.</param>
        /// <param name="id">The unique aggregate id.</param>
        /// <param name="initializer">The aggregate initializer.</param>
        public static TAggregate Create<TAggregate>(this IStoreAggregates aggregateStore, Guid id, Action<TAggregate> initializer)
            where TAggregate : Aggregate
        {
            Verify.NotNull(aggregateStore, "aggregateStore");
            Verify.NotNull(initializer, "initializer");

            var aggregate = aggregateStore.Get<TAggregate>(id);
            if (aggregate.Version > 0)
                throw new InvalidOperationException(Exceptions.AggregateAlreadyExists.FormatWith(typeof(TAggregate), id));

            return aggregateStore.Create(aggregate, initializer);
        }

        /// <summary>
        /// Gets or creates an aggregate of type <typeparamref name="TAggregate"/> identified by <paramref name="id"/>.
        /// </summary>
        /// <typeparam name="TAggregate">The type of aggregate to retrieve or create.</typeparam>
        /// <param name="aggregateStore">The aggregate store to extend.</param>
        /// <param name="id">The unique aggregate id.</param>
        /// <param name="initializer">The aggregate initializer.</param>
        public static TAggregate GetOrCreate<TAggregate>(this IStoreAggregates aggregateStore, Guid id, Action<TAggregate> initializer)
            where TAggregate : Aggregate
        {
            Verify.NotNull(aggregateStore, "aggregateStore");
            Verify.NotNull(initializer, "initializer");

            var aggregate = aggregateStore.Get<TAggregate>(id);
            if (aggregate.Version > 0)
                return aggregate;

            return aggregateStore.Create(aggregate, initializer);
        }

        /// <summary>
        /// Initializes a newly created aggregate instance.
        /// </summary>
        /// <typeparam name="TAggregate">The type of aggregate to initialize.</typeparam>
        /// <param name="aggregateStore">The aggregate repository to which the aggregate is to be saved.</param>
        /// <param name="aggregate">The newly created aggregate.</param>
        /// <param name="initializer">The aggregate initializer.</param>
        private static TAggregate Create<TAggregate>(this IStoreAggregates aggregateStore, TAggregate aggregate, Action<TAggregate> initializer)
            where TAggregate : Aggregate
        {
            var context = CommandContext.GetCurrent();
            using (var createContext = new CommandContext(aggregate.Id, context.Headers, new CommandEnvelope(aggregate.Id, context.Command)))
            {
                initializer.Invoke(aggregate);

                return (TAggregate)aggregateStore.Save(aggregate, createContext).Aggregate;
            }
        }
    }
}
