using System;
using Spark.Infrastructure;
using Spark.Infrastructure.Commanding;

namespace Example.Domain.Commands
{
    public sealed class RegisterClient : Command
    {
        public String Name { get; private set; }

        public RegisterClient(String name)
        {
            Verify.NotNullOrWhiteSpace(name, "name");

            Name = name;
        }
    }
}
