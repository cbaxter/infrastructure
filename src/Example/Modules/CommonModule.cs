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
            base.Load(builder);

            builder.RegisterType<ServiceMessageFactory>().As<ICreateMessages>().SingleInstance();
            builder.RegisterType<TypeLocator>().As<ILocateTypes>().SingleInstance();
            builder.RegisterType<NewtonsoftJsonSerializer>().As<ISerializeObjects>().SingleInstance();
            builder.Register(context => new AutofacServiceProvider(context.Resolve<ILifetimeScope>())).As<IServiceProvider>().SingleInstance();
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
