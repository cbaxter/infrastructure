﻿using System;
using System.Runtime.Serialization;
using Spark.Infrastructure;
using Spark.Infrastructure.Eventing;

namespace Example.Domain.Events
{
    [DataContract]
    public sealed class ClientRegistered : Event
    {
        [DataMember(Name = "n")]
        public String Name { get; private set; }

        public ClientRegistered(String name)
        {
            Verify.NotNullOrWhiteSpace(name, "name");

            Name = name;
        }
    }
}