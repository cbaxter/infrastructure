using System;
using System.Diagnostics;
using System.Threading;
using Autofac;
using Example.Domain.Commands;
using Example.Modules;
using Spark.Infrastructure;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.EventStore.Sql.Dialects;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            GuidStrategy.Initialize(SqlServerSequentialGuid.NewGuid);

            var count = 1000000;
            var builder = new ContainerBuilder();
            var benchmarkHook = new BenchmarkHook(count);

            builder.RegisterModule<CommandingModule>();
            builder.RegisterInstance(benchmarkHook).As<PipelineHook>();

            var container = builder.Build();

            var commandReceiver = container.Resolve<CommandReceiver>();
            var commandPublisher = container.Resolve<IPublishCommands>();
            var snapshotStore = container.Resolve<IStoreSnapshots>();
            var eventStore = container.Resolve<IStoreEvents>();
            
            Console.WriteLine("Purging event store...");
            eventStore.Purge();

            Console.WriteLine("Starting performance test...");
            Console.WriteLine();

            for (var i = 1; i <= count; i++)
                commandPublisher.Publish(GuidStrategy.NewGuid(), new RegisterClient("User #" + i.ToString("{0:00000}")));

            benchmarkHook.WaitForCommandDrain();
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
