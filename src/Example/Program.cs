using System;
using System.Data.SqlClient;
using System.Threading;
using Autofac;
using Example.Domain.Commands;
using Example.Modules;
using Spark.Infrastructure;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.EventStore.Dialects;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var listeners = System.Diagnostics.Trace.Listeners;
            listeners[0].Write("Test");
            var builder = new ContainerBuilder();
            builder.RegisterModule<CommandingModule>();

            GuidStrategy.Initialize(SqlServerSequentialGuid.NewGuid);

            var container = builder.Build();

            var commandReceiver = container.Resolve<CommandReceiver>();
            var commandPublisher = container.Resolve<IPublishCommands>();
            var snapshotStore = container.Resolve<IStoreSnapshots>();
            var eventStore = container.Resolve<IStoreEvents>();

            var connection = new SqlConnection("Data Source=(local); Initial Catalog=Infrastructure; Integrated Security=true;");
            var command = new SqlCommand("SELECT SUM (row_count) FROM sys.dm_db_partition_stats WHERE object_id=OBJECT_ID('Commit') AND (index_id=0 or index_id=1)", connection);
            var count = 100000;
            var commits = 0L;

            Console.WriteLine("Initializing event store...");
            snapshotStore.Initialize();
            eventStore.Initialize();
            
            Console.WriteLine("Purging event store...");
            eventStore.Purge();

            Console.WriteLine("Starting performance test...");
            DateTime start = DateTime.Now;

            for (var i = 1; i <= count; i++)
                commandPublisher.Publish(GuidStrategy.NewGuid(), new RegisterClient("User #" + i.ToString("{0:00000}")));

            while (commits < count)
            {
                connection.Open();

                try
                {
                    commits = (Int64)command.ExecuteScalar();
                    Console.Write("\rCommits: " + commits);
                }
                finally
                {
                    connection.Close();
                }

                Thread.Sleep(500);
            }

            DateTime end = DateTime.Now;

            Console.WriteLine();
            Console.WriteLine((count / end.Subtract(start).TotalSeconds) + @" / sec");
        }
    }
}
