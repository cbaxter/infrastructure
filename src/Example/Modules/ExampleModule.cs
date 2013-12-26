using Autofac;
using Spark.Example.Services;
using Module = Autofac.Module;

namespace Spark.Example.Modules
{
    public sealed class ExampleModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register example specific types.
            builder.RegisterType<AccountNumberGenerator>().As<IGenerateAccountNumbers>().SingleInstance();
        }
    }
}
