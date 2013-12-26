using System;
using Autofac;
using Spark.Messaging;
using Spark.Serialization;
using Module = Autofac.Module;

namespace Spark.Example.Modules
{
    public sealed class CommonModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register common infrastructure.
            builder.Register(context => new AutofacServiceProvider(context.Resolve<ILifetimeScope>())).As<IServiceProvider>().SingleInstance();
            builder.RegisterType<NewtonsoftJsonSerializer>().As<ISerializeObjects>().SingleInstance();
            builder.RegisterType<ServiceMessageFactory>().As<ICreateMessages>().SingleInstance();
            builder.RegisterType<TypeLocator>().As<ILocateTypes>().SingleInstance();
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
