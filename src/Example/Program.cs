﻿using System;
using System.Diagnostics;
using System.Threading;
using Autofac;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Cqrs.Eventing.Sagas.Sql;
using Spark.Data.SqlClient;
using Spark.EventStore;
using Spark.EventStore.Sql;
using Spark.Example.Domain.Commands;
using Spark.Example.Modules;
using Spark.Messaging;
using PipelineHook = Spark.Cqrs.Domain.PipelineHook;

namespace Example
{
    class Program
    {
        static void Main()
        {
            GuidStrategy.Initialize(SqlSequentialGuid.NewGuid);

            var count = 50000;
            var builder = new ContainerBuilder();
            var benchmarkHook = new BenchmarkHook(count);

            builder.RegisterModule<CommonModule>();
            builder.RegisterModule<CommandingModule>();
            builder.RegisterModule<EventingModule>();
            builder.RegisterInstance(benchmarkHook).As<PipelineHook>();

            var container = builder.Build();
            var commandPublisher = container.Resolve<IPublishCommands>();
            var snapshotStore = container.Resolve<IStoreSnapshots>();
            var eventStore = container.Resolve<IStoreEvents>();
            var sagaStore = container.Resolve<IStoreSagas>();

            Console.WriteLine("Purging event store...");
            eventStore.Purge();

            Console.WriteLine("Purging snapshot store...");
            snapshotStore.Purge();

            Console.WriteLine("Purging saga store...");
            sagaStore.Purge();

            Console.WriteLine("Starting performance test...");
            Console.WriteLine();

            for (var i = 1; i <= count; i++)
                commandPublisher.Publish(GuidStrategy.NewGuid(), new RegisterClient("User #" + i.ToString("{0:00000}")));

            benchmarkHook.WaitForCommandDrain();

            ((SqlSnapshotStore)container.Resolve<IStoreSnapshots>()).Dispose();
            ((SqlEventStore)container.Resolve<IStoreEvents>()).Dispose();
            (container.Resolve<IStoreSagas>()).Dispose();

            ((IDisposable)container.Resolve<IReceiveMessages<EventEnvelope>>()).Dispose();
            container.Resolve<MessageReceiver<EventEnvelope>>().Dispose();
        }

        public class BenchmarkHook : PipelineHook
        {
            private readonly ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            private readonly Int32 expectedCommands;
            private readonly Timer timer;
            private Int64 savedCommands;
            private DateTime startTime;
            private Boolean started;

            public BenchmarkHook(Int32 expectedCommands)
            {
                this.expectedCommands = expectedCommands;
                this.timer = new Timer(WriteThroughput, null, 0, 500);
            }

            private void WriteThroughput(Object state)
            {
                var current = Interlocked.Read(ref savedCommands);
                if (current > 0 && current < expectedCommands)
                {
                    var elapsedTime = DateTime.Now.Subtract(startTime);

                    using (var process = Process.GetCurrentProcess())
                        Console.Write("\r{0}: {1:0000000} @ {2:F2}/sec ({3})", elapsedTime, current, Math.Round(current / elapsedTime.TotalSeconds, 2), process.Threads.Count);
                }
            }

            public override void PostSave(Aggregate aggregate, Commit commit, Exception error)
            {
                if (!started)
                {
                    startTime = DateTime.Now;
                    started = true;
                }

                if (Interlocked.Increment(ref savedCommands) == expectedCommands)
                {
                    var elapsedTime = DateTime.Now.Subtract(startTime);

                    Console.Write("\r{0}: {1:0000000} @ {2:F2}/sec", elapsedTime, expectedCommands, Math.Round(expectedCommands / elapsedTime.TotalSeconds, 2));
                    Console.WriteLine();
                    Console.WriteLine();
                    manualResetEvent.Set();
                }
            }

            public void WaitForCommandDrain()
            {
                manualResetEvent.WaitOne();
            }
        }
    }
}
