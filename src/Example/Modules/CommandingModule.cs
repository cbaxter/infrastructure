using System;
using System.Collections.Generic;
using System.Reflection;
using Autofac;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.EventStore;
using Spark.EventStore.Sql;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Mappings;
using Spark.Messaging;
using Spark.Serialization;
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
            builder.RegisterType<ServiceMessageFactory>().As<ICreateMessages>().SingleInstance();

            builder.RegisterType<CommandPublisher>().As<IPublishCommands>().SingleInstance();

            builder.RegisterType<TypeLocator>().As<ILocateTypes>().SingleInstance();
            builder.RegisterType<NewtonsoftJsonSerializer>().As<ISerializeObjects>().SingleInstance();
            builder.Register(context => new AutofacServiceProvider(context.Resolve<ILifetimeScope>())).As<IServiceProvider>().SingleInstance();


            builder.RegisterType<AggregateUpdater>().As<IApplyEvents>().SingleInstance();
            builder.RegisterType<AggregateStore>().Named<IStoreAggregates>("AggregateStore").SingleInstance();
            builder.Register(context => new SqlEventStore(context.Resolve<ISerializeObjects>(), "eventStore")).As<IStoreEvents>().SingleInstance();
            builder.Register(context => new SqlSnapshotStore(context.Resolve<ISerializeObjects>(), "eventStore")).As<IStoreSnapshots>().SingleInstance();

            builder.RegisterType<MessageReceiver<CommandEnvelope>>().AsSelf().SingleInstance().AutoActivate();
            builder.RegisterType<CommandProcessor>().As<IProcessMessages<CommandEnvelope>>().SingleInstance();
            builder.RegisterType<CommandHandlerRegistry>().As<IRetrieveCommandHandlers>().SingleInstance();

            builder.RegisterDecorator<IStoreAggregates>((context, aggregateStore) => new CachedAggregateStore(aggregateStore), "AggregateStore").Named<IStoreAggregates>("CachedAggregateStore").SingleInstance();
            builder.RegisterDecorator<IStoreAggregates>((context, aggregateStore) => new HookableAggregateStore(aggregateStore, context.Resolve<IEnumerable<PipelineHook>>()), "CachedAggregateStore").As<IRetrieveAggregates>().As<IStoreAggregates>().SingleInstance();

            //TODO: Cleanup...
            builder.RegisterType<EventDispatcher>().As<PipelineHook>().SingleInstance();
            builder.RegisterType<EventPublisher>().As<IPublishEvents>().SingleInstance();
            builder.RegisterType<EventHandlerRegistry>().As<IRetrieveEventHandlers>().SingleInstance();
            builder.RegisterType<EventProcessor>().As<IProcessMessages<EventEnvelope>>().SingleInstance();
            builder.RegisterType<MessageReceiver<EventEnvelope>>().AsSelf().SingleInstance().AutoActivate();
            builder.RegisterType<BlockingCollectionMessageBus<EventEnvelope>>().As<ISendMessages<EventEnvelope>>().As<IReceiveMessages<EventEnvelope>>().SingleInstance();
            builder.RegisterAssemblyTypes(AppDomain.CurrentDomain.GetAssemblies()).Where(type => type.GetCustomAttribute<EventHandlerAttribute>() != null);
        }

        private sealed class AutofacServiceProvider : IServiceProvider
        {
            private readonly ILifetimeScope lifetimeScope;

            public AutofacServiceProvider(ILifetimeScope lifetimeScope)
            {
                Verify.NotNull(lifetimeScope, "lifetimeScope");

                this.lifetimeScope = lifetimeScope;
            }

            public object GetService(Type serviceType)
            {
                return lifetimeScope.Resolve(serviceType);
            }
        }
    }
}
