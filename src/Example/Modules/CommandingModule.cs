using System.Collections.Generic;
using Autofac;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing;
using Spark.EventStore;
using Spark.EventStore.Sql;
using Spark.EventStore.Sql.Dialects;
using Spark.Example.Benchmarks;
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
            builder.RegisterType<CommandHandlerRegistry>().As<IRetrieveCommandHandlers>().SingleInstance();
            builder.RegisterType<CommandPublisher>().Named<IPublishCommands>("CommandPublisher").SingleInstance();
            builder.RegisterType<CommandProcessor>().Named<IProcessMessages<CommandEnvelope>>("CommandProcessor").SingleInstance();

            // Register data store infrastructure.
            builder.RegisterType<SqlEventStoreDialect>().AsSelf().As<IEventStoreDialect>().SingleInstance();
            builder.RegisterType<SqlEventStore>().AsSelf().Named<IStoreEvents>("EventStore").SingleInstance();

            builder.RegisterType<SqlSnapshotStoreDialect>().AsSelf().As<ISnapshotStoreDialect>().SingleInstance();
            builder.RegisterType<SqlSnapshotStore>().AsSelf().Named<IStoreSnapshots>("SnapshotStore").SingleInstance();

            builder.RegisterType<AggregateUpdater>().AsSelf().As<IApplyEvents>().SingleInstance();
            builder.RegisterType<AggregateStore>().AsSelf().Named<IStoreAggregates>("AggregateStore").SingleInstance();

            // Register decorators.
            builder.RegisterType<CommandHook>().AsSelf().As<PipelineHook>().SingleInstance();
            builder.RegisterType<EventDispatcher>().AsSelf().As<PipelineHook>().SingleInstance();
            builder.RegisterDecorator<IStoreAggregates>((context, aggregateStore) => new CachedAggregateStore(aggregateStore), "AggregateStore").Named<IStoreAggregates>("CachedAggregateStore").SingleInstance();
            builder.RegisterDecorator<IStoreAggregates>((context, aggregateStore) => new HookableAggregateStore(aggregateStore, context.Resolve<IEnumerable<PipelineHook>>()), "CachedAggregateStore").As<IRetrieveAggregates>().As<IStoreAggregates>().SingleInstance();
            builder.RegisterDecorator<IStoreSnapshots>((context, snapshotStore) => new BenchmarkedSnapshotStore(snapshotStore, context.Resolve<Statistics>()), "SnapshotStore").As<IStoreSnapshots>().SingleInstance();
            builder.RegisterDecorator<IStoreEvents>((context, eventStore) => new BenchmarkedEventStore(eventStore, context.Resolve<Statistics>()), "EventStore").As<IStoreEvents>().SingleInstance();
            builder.RegisterDecorator<IPublishCommands>((context, commandPublisher) => new CommandPublisherWrapper(commandPublisher, context.Resolve<Statistics>()), "CommandPublisher").As<IPublishCommands>().SingleInstance();
            builder.RegisterDecorator<IProcessMessages<CommandEnvelope>>((context, commandProcessor) => new CommandProcessorWrapper(commandProcessor, context.Resolve<Statistics>()), "CommandProcessor").As<IProcessMessages<CommandEnvelope>>().SingleInstance();
        }
    }
}
