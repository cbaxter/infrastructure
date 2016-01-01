using System;
using System.Collections.Generic;
using System.Messaging;
using Autofac;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing;
using Spark.EventStore;
using Spark.EventStore.Sql;
using Spark.EventStore.Sql.Dialects;
using Spark.Example.Benchmarks;
using Spark.Messaging;
using Spark.Messaging.Msmq;
using Spark.Serialization;
using Module = Autofac.Module;

namespace Spark.Example.Modules
{
    public sealed class CommandingModule : Module
    {
        public static String MessageQueuePath => @".\private$\spark.infrastructure.example.commands";

        public static MessageBusType MessageBusType { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            MessageQueue.EnableConnectionCache = true;

            // Register the specified message bus infrastructure.
            switch (MessageBusType)
            {
                case MessageBusType.Inline:
                    builder.RegisterType<DirectMessageSender<CommandEnvelope>>().AsSelf().As<ISendMessages<CommandEnvelope>>().SingleInstance().AutoActivate();
                    break;
                case MessageBusType.Optimistic:
                    builder.RegisterType<OptimisticMessageSender<CommandEnvelope>>().AsSelf().As<ISendMessages<CommandEnvelope>>().SingleInstance();
                    break;
                case MessageBusType.MicrosoftMessageQueuing:
                    PurgeMessageQueue("processing", "poison");
                    builder.Register(resolver => new MessageSender<CommandEnvelope>(MessageQueuePath, resolver.Resolve<ISerializeObjects>())).AsSelf().As<ISendMessages<CommandEnvelope>>().SingleInstance().AutoActivate();
                    builder.Register(resolver => new MessageReceiver<CommandEnvelope>(MessageQueuePath, resolver.Resolve<ISerializeObjects>(), resolver.Resolve<IProcessMessages<CommandEnvelope>>())).AsSelf().SingleInstance().AutoActivate();
                    break;
                default:
                    throw new NotSupportedException();
            }

            // Register common commanding infrastructure. 
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

        /// <summary>
        /// Purge the underlying Microsoft message queue.
        /// </summary>
        /// <param name="subqueues">The known subqueues to purge.</param>
        private static void PurgeMessageQueue(params String[] subqueues)
        {
            using (var queue = new MessageQueue(MessageQueuePath))
                queue.Purge();

            foreach (var subqueue in subqueues)
            {
                using (var queue = new MessageQueue(MessageQueuePath + ";" + subqueue))
                    queue.Purge();
            }
        }
    }
}
