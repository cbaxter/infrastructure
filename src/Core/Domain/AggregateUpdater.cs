using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Spark.Infrastructure.Domain.Mappings;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Logging;

namespace Spark.Infrastructure.Domain
{
    public sealed class AggregateUpdater : IApplyEvents //TODO: IRetrieveApplyMethods? (i.e., make similar to CommandHandlerRegistry...)
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private static readonly Action<Aggregate, Event> VoidApplyMethod = (aggregate, e) => { };
        private readonly ReadOnlyDictionary<Type, ApplyMethodCollection> knownApplyMethods;

        public AggregateUpdater(ILocateTypes typeLocator)
        {
            Verify.NotNull(typeLocator, "typeLocator");

            knownApplyMethods = new ReadOnlyDictionary<Type, ApplyMethodCollection>(DiscoverAggregates(typeLocator));
        }

        private static IDictionary<Type, ApplyMethodCollection> DiscoverAggregates(ILocateTypes typeLocator)
        {
            var aggregateTypes = typeLocator.GetTypes(type => !type.IsAbstract && type.IsClass && type.DerivesFrom(typeof(Aggregate)));
            var result = new Dictionary<Type, ApplyMethodCollection>();

            foreach (var aggregateType in aggregateTypes)
                result.Add(aggregateType, DiscoverApplyMethods(aggregateType));

            return result;
        }

        private static ApplyMethodCollection DiscoverApplyMethods(Type aggregateType)
        {
            if (aggregateType.GetConstructor(Type.EmptyTypes) == null)
                throw new MappingException("");

            var applyMethodMappings = aggregateType.GetCustomAttributes<ApplyByStrategyAttribute>().ToArray();
            if (applyMethodMappings.Length > 1)
                throw new MappingException(); //TODO: set message

            return (applyMethodMappings.Length == 0 ? ApplyByStrategyAttribute.Default : applyMethodMappings[0]).GetApplyMethods(aggregateType);
        }

        public void Apply(Event e, Aggregate aggregate)
        {
            Verify.NotNull(e, "e");
            Verify.NotNull(aggregate, "aggregate");

            var applyMethods = GetKnownApplyMethods(aggregate);
            var applyMethod = GetApplyMethod(applyMethods, e);

            Log.DebugFormat("Applying event {0} to aggregate {1}", e, aggregate);
            
            applyMethod(aggregate, e);
        }

        private ApplyMethodCollection GetKnownApplyMethods(Aggregate aggregate)
        {
            Type aggregateType = aggregate.GetType();
            ApplyMethodCollection applyMethods;

            if (!knownApplyMethods.TryGetValue(aggregateType, out applyMethods))
                throw new MappingException();

            return applyMethods;
        }

        private static Action<Aggregate, Event> GetApplyMethod(ApplyMethodCollection applyMethods, Event e)
        {
            Action<Aggregate, Event> applyMethod;
            Type eventType = e.GetType();

            if (applyMethods.TryGetValue(eventType, out applyMethod))
                return applyMethod;
            
            if (!applyMethods.ApplyOptional)
                throw new MappingException();

            Log.Debug("sinregdhfgjkdfg");

            return VoidApplyMethod;
        }
    }
}