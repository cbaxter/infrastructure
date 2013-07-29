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

            builder.RegisterType<EventPublisher>().As<IPublishEvents>().SingleInstance();
            builder.RegisterType<EventHandlerRegistry>().As<IRetrieveEventHandlers>().SingleInstance();
            builder.RegisterType<EventProcessor>().As<IProcessMessages<EventEnvelope>>().SingleInstance();
            builder.RegisterType<MessageReceiver<EventEnvelope>>().AsSelf().SingleInstance().AutoActivate();
            builder.RegisterType<BlockingCollectionMessageBus<EventEnvelope>>().As<ISendMessages<EventEnvelope>>().As<IReceiveMessages<EventEnvelope>>().SingleInstance();
            //builder.RegisterType<InlineMessageBus<EventEnvelope>>().As<ISendMessages<EventEnvelope>>().SingleInstance();
            builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies()).Where(type => type.GetCustomAttribute<EventHandlerAttribute>() != null);

            builder.RegisterType<SqlSagaStoreDialect>().As<ISagaStoreDialect>().SingleInstance();
            builder.RegisterType<SqlSagaStore>().Named<IStoreSagas>("SagaStore").SingleInstance();

            builder.Register(container => new TimeoutPipelineHook(container.ResolveNamed<IStoreSagas>("SagaStore"))).As<PipelineHook>().SingleInstance();
            builder.RegisterDecorator<IStoreSagas>((context, sagaStore) => new CachedSagaStore(sagaStore), "SagaStore").Named<IStoreSagas>("CachedSagaStore").SingleInstance();
            builder.RegisterDecorator<IStoreSagas>((context, sagaStore) => new HookableSagaStore(sagaStore, context.Resolve<IEnumerable<PipelineHook>>()), "CachedSagaStore").As<IStoreSagas>().SingleInstance();
        }
    }
}
