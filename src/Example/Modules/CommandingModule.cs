using System;
using System.Collections.Generic;
using Autofac;
using Spark.Infrastructure;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.EventStore.Sql;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Messaging;
using Spark.Infrastructure.Serialization;

namespace Example.Modules
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
            builder.RegisterType<BlockingCollectionMessageBus<EventEnvelope>>().As<ISendMessages<EventEnvelope>>().As<IReceiveMessages<EventEnvelope>>().SingleInstance();
            builder.RegisterType<EventPublisher>().As<IPublishEvents>().SingleInstance();
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
