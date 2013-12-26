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
            // Register underlying commanding infrastructure.
            builder.RegisterType<BlockingCollectionMessageBus<CommandEnvelope>>().AsSelf().As<ISendMessages<CommandEnvelope>>().As<IReceiveMessages<CommandEnvelope>>().SingleInstance();
            builder.RegisterType<MessageReceiver<CommandEnvelope>>().AsSelf().SingleInstance().AutoActivate();
            builder.RegisterType<CommandProcessor>().As<IProcessMessages<CommandEnvelope>>().SingleInstance();
            builder.RegisterType<CommandHandlerRegistry>().As<IRetrieveCommandHandlers>().SingleInstance();
            builder.RegisterType<CommandPublisher>().As<IPublishCommands>().SingleInstance();

            // Register data store infrastructure.
            builder.RegisterType<SqlEventStoreDialect>().AsSelf().As<IEventStoreDialect>().SingleInstance();
            builder.RegisterType<SqlEventStore>().AsSelf().As<IStoreEvents>().SingleInstance();

            builder.RegisterType<SqlSnapshotStoreDialect>().AsSelf().As<ISnapshotStoreDialect>().SingleInstance();
            builder.RegisterType<SqlSnapshotStore>().AsSelf().As<IStoreSnapshots>().SingleInstance();

            builder.RegisterType<AggregateUpdater>().AsSelf().As<IApplyEvents>().SingleInstance();
            builder.RegisterType<AggregateStore>().AsSelf().Named<IStoreAggregates>("AggregateStore").SingleInstance();

            // Register decorators.
            builder.RegisterType<EventDispatcher>().AsSelf().As<PipelineHook>().SingleInstance();
            builder.RegisterDecorator<IStoreAggregates>((context, aggregateStore) => new CachedAggregateStore(aggregateStore), "AggregateStore").Named<IStoreAggregates>("CachedAggregateStore").SingleInstance();
            builder.RegisterDecorator<IStoreAggregates>((context, aggregateStore) => new HookableAggregateStore(aggregateStore, context.Resolve<IEnumerable<PipelineHook>>()), "CachedAggregateStore").As<IRetrieveAggregates>().As<IStoreAggregates>().SingleInstance();
        }
    }
}
