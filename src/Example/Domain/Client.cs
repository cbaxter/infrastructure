using Example.Domain.Commands;
using Spark.Infrastructure.Domain;

namespace Example.Domain
{
    public sealed class Client : Aggregate
    {
        public void Handle(RegisterClient command)
        {
            //TODO: Should really enforce new aggregate... CreateWith(...)
        }
    }
}
