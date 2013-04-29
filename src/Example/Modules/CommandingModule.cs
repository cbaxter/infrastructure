using System;
using Autofac;
using Spark.Infrastructure;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.EventStore;
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
            builder.RegisterType<BinarySerializer>().As<ISerializeObjects>().SingleInstance();
            builder.Register(context => new AutofacServiceProvider(context.Resolve<ILifetimeScope>())).As<IServiceProvider>().SingleInstance();


            builder.RegisterType<AggregateUpdater>().As<IApplyEvents>().SingleInstance();
            builder.RegisterType<AggregateStore>().As<IRetrieveAggregates>().As<IStoreAggregates>().SingleInstance();
            builder.Register(context => new DbEventStore("eventStore", context.Resolve<ISerializeObjects>())).As<IStoreEvents>().SingleInstance();
            builder.Register(context => new DbSnapshotStore("eventStore", context.Resolve<ISerializeObjects>())).As<IStoreSnapshots>().SingleInstance();


            builder.RegisterType<CommandReceiver>().AsSelf().SingleInstance();
            builder.RegisterType<CommandProcessor>().As<IProcessCommands>().SingleInstance();
            builder.RegisterType<CommandHandlerRegistry>().As<IRetrieveCommandHandlers>().SingleInstance();
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
