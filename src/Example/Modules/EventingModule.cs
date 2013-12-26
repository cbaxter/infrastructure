using System;
using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Mappings;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Cqrs.Eventing.Sagas.Sql;
using Spark.Cqrs.Eventing.Sagas.Sql.Dialects;
using Spark.Messaging;
using Module = Autofac.Module;

namespace Spark.Example.Modules
{
    public sealed class EventingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            // Register underlying eventing infrastructure.
            builder.RegisterType<BlockingCollectionMessageBus<EventEnvelope>>().AsSelf().As<ISendMessages<EventEnvelope>>().As<IReceiveMessages<EventEnvelope>>().SingleInstance();
            builder.RegisterType<MessageReceiver<EventEnvelope>>().AsSelf().SingleInstance().AutoActivate();
            builder.RegisterType<EventProcessor>().As<IProcessMessages<EventEnvelope>>().SingleInstance();
            builder.RegisterType<EventHandlerRegistry>().As<IRetrieveEventHandlers>().SingleInstance();
            builder.RegisterType<EventPublisher>().As<IPublishEvents>().SingleInstance();

            // Register all event handlers.
            builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies()).Where(type => type.GetCustomAttribute<EventHandlerAttribute>() != null);

            // Register data store infrastructure.
            builder.RegisterType<SqlSagaStoreDialect>().AsSelf().As<ISagaStoreDialect>().SingleInstance();
            builder.RegisterType<SqlSagaStore>().AsSelf().Named<IStoreSagas>("SagaStore").SingleInstance();

            // Register decorators.
            builder.Register(container => new TimeoutDispatcher(container.ResolveNamed<IStoreSagas>("SagaStore"), container.Resolve<IPublishEvents>())).As<PipelineHook>().SingleInstance();
            builder.RegisterDecorator<IStoreSagas>((context, sagaStore) => new CachedSagaStore(sagaStore), "SagaStore").Named<IStoreSagas>("CachedSagaStore").SingleInstance();
            builder.RegisterDecorator<IStoreSagas>((context, sagaStore) => new HookableSagaStore(sagaStore, context.Resolve<IEnumerable<PipelineHook>>()), "CachedSagaStore").As<IStoreSagas>().SingleInstance();
        }
    }
}
