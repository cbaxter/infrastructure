
namespace Spark.Infrastructure.Eventing.Sagas
{
    public sealed class SagaEventHandler : EventHandler
    {
        public SagaEventHandler(EventHandler eventHandler)
            : base(eventHandler)
        { }
    }
}
