using System;
using System.Collections.Generic;
using System.Messaging;
using System.Reflection;
using Autofac;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Mappings;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Cqrs.Eventing.Sagas.Sql;
using Spark.Cqrs.Eventing.Sagas.Sql.Dialects;
using Spark.Example.Benchmarks;
using Spark.Messaging;
using Spark.Messaging.Msmq;
using Spark.Serialization;
using Module = Autofac.Module;

namespace Spark.Example.Modules
{
    public sealed class EventingModule : Module
    {
        public static String MessageQueuePath => @".\private$\spark.infrastructure.example.events";

        public static MessageBusType MessageBusType { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            MessageQueue.EnableConnectionCache = true;

            // Register the specified message bus infrastructure.
            switch (MessageBusType)
            {
                case MessageBusType.Inline:
                    builder.RegisterType<DirectMessageSender<EventEnvelope>>().AsSelf().As<ISendMessages<EventEnvelope>>().SingleInstance().AutoActivate();
                    break;
                case MessageBusType.Optimistic:
                    builder.RegisterType<OptimisticMessageSender<EventEnvelope>>().AsSelf().As<ISendMessages<EventEnvelope>>().SingleInstance();
                    break;
                case MessageBusType.MicrosoftMessageQueuing:
                    PurgeMessageQueue("processing", "poison");
                    builder.Register(resolver => new MessageSender<EventEnvelope>(MessageQueuePath, resolver.Resolve<ISerializeObjects>())).AsSelf().As<ISendMessages<EventEnvelope>>().SingleInstance().AutoActivate();
                    builder.Register(resolver => new MessageReceiver<EventEnvelope>(MessageQueuePath, resolver.Resolve<ISerializeObjects>(), resolver.Resolve<IProcessMessages<EventEnvelope>>())).AsSelf().SingleInstance().AutoActivate();
                    break;
                default:
                    throw new NotSupportedException();
            }

            // Register common eventing infrastructure. 
            builder.RegisterType<TimeoutDispatcher>().As<PipelineHook>().SingleInstance();
            builder.RegisterType<EventHandlerRegistry>().As<IRetrieveEventHandlers>().SingleInstance();
            builder.RegisterType<EventPublisher>().Named<IPublishEvents>("EventPublisher").SingleInstance();
            builder.RegisterType<EventProcessor>().Named<IProcessMessages<EventEnvelope>>("EventProcessor").SingleInstance();

            // Register all event handlers.
            builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies()).Where(type => type.GetCustomAttribute<EventHandlerAttribute>() != null);

            // Register data store infrastructure.
            builder.RegisterType<SqlSagaStoreDialect>().AsSelf().As<ISagaStoreDialect>().SingleInstance();
            builder.RegisterType<SqlSagaStore>().AsSelf().Named<IStoreSagas>("SagaStore").SingleInstance();

            // Register decorators.
            builder.RegisterDecorator<IStoreSagas>((context, sagaStore) => new BenchmarkedSagaStore(sagaStore, context.Resolve<Statistics>()), "SagaStore").Named<IStoreSagas>("BenchmarkedSagaStore").SingleInstance();
            builder.RegisterDecorator<IStoreSagas>((context, sagaStore) => new CachedSagaStore(sagaStore), "BenchmarkedSagaStore").Named<IStoreSagas>("CachedSagaStore").SingleInstance();
            builder.RegisterDecorator<IStoreSagas>((context, sagaStore) => new HookableSagaStore(sagaStore, context.Resolve<IEnumerable<PipelineHook>>()), "CachedSagaStore").As<IStoreSagas>().SingleInstance();
            builder.RegisterDecorator<IPublishEvents>((context, eventPublisher) => new EventPublisherWrapper(eventPublisher, context.Resolve<Statistics>()), "EventPublisher").As<IPublishEvents>().SingleInstance();
            builder.RegisterDecorator<IProcessMessages<EventEnvelope>>((context, eventProcessor) => new EventProcessorWrapper(eventProcessor, context.Resolve<Statistics>()), "EventProcessor").As<IProcessMessages<EventEnvelope>>().SingleInstance();
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
