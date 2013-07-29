using System.Collections.Generic;
using Autofac;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing;
using Spark.EventStore;
using Spark.EventStore.Sql;
using Spark.EventStore.Sql.Dialects;
using Spark.Messaging;
using Module = Autofac.Module;

namespace Spark.Example.Modules
{
    public sealed class CommandingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            // Register Common Types (i.e., Sending or Receiving).
            builder.RegisterType<BlockingCollectionMessageBus<CommandEnvelope>>().As<ISendMessages<CommandEnvelope>>().As<IReceiveMessages<CommandEnvelope>>().SingleInstance();

            builder.RegisterType<CommandPublisher>().As<IPublishCommands>().SingleInstance();

            
            builder.RegisterType<SqlEventStoreDialect>().As<IEventStoreDialect>().SingleInstance();
            builder.RegisterType<SqlEventStore>().As<IStoreEvents>().SingleInstance();

            builder.RegisterType<SqlSnapshotStoreDialect>().As<ISnapshotStoreDialect>().SingleInstance();
            builder.RegisterType<SqlSnapshotStore>().As<IStoreSnapshots>().SingleInstance();

            builder.RegisterType<AggregateUpdater>().As<IApplyEvents>().SingleInstance();
            builder.RegisterType<AggregateStore>().Named<IStoreAggregates>("AggregateStore").SingleInstance();

            builder.RegisterType<MessageReceiver<CommandEnvelope>>().AsSelf().SingleInstance().AutoActivate();
            builder.RegisterType<CommandProcessor>().As<IProcessMessages<CommandEnvelope>>().SingleInstance();
            builder.RegisterType<CommandHandlerRegistry>().As<IRetrieveCommandHandlers>().SingleInstance();

            builder.RegisterType<EventDispatcher>().As<PipelineHook>().SingleInstance();
            builder.RegisterDecorator<IStoreAggregates>((context, aggregateStore) => new CachedAggregateStore(aggregateStore), "AggregateStore").Named<IStoreAggregates>("CachedAggregateStore").SingleInstance();
            builder.RegisterDecorator<IStoreAggregates>((context, aggregateStore) => new HookableAggregateStore(aggregateStore, context.Resolve<IEnumerable<PipelineHook>>()), "CachedAggregateStore").As<IRetrieveAggregates>().As<IStoreAggregates>().SingleInstance();
        }
    }
}
