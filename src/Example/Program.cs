using System;
using System.Collections.Generic;
using System.Threading;
using Autofac;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Cqrs.Eventing.Sagas.Sql;
using Spark.Data.SqlClient;
using Spark.EventStore;
using Spark.EventStore.Sql;
using Spark.Example.Domain;
using Spark.Example.Domain.Commands;
using Spark.Example.Modules;
using Spark.Messaging;

namespace Spark.Example
{
    /// <summary>
    /// Basic test program used to evaluate infrastructure performance.
    /// </summary>
    internal static class Program
    {
        private const Int32 NumberOfCommandsToPublish = 5000;

        /// <summary>
        /// The main entry point for the test program.
        /// </summary>
        internal static void Main()
        {
            var container = Initialize();

            Purge(container);
            PublishCommands(container);

            WaitForCompletion(container);
        }

        /// <summary>
        /// Initialize the underlying IoC container.
        /// </summary>
        private static IContainer Initialize()
        {
            var builder = new ContainerBuilder();

            GuidStrategy.Initialize(SqlSequentialGuid.NewGuid);

            builder.RegisterModule<CommonModule>();
            builder.RegisterModule<CommandingModule>();
            builder.RegisterModule<EventingModule>();
            builder.RegisterModule<ExampleModule>();

            return builder.Build();
        }

        /// <summary>
        /// Purge all existing data to ensure a clean slate prior to running the test program.
        /// </summary>
        /// <param name="container">The test IoC container.</param>
        private static void Purge(IComponentContext container)
        {
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
        }

        /// <summary>
        /// Publish a set of pre-defined test commands to evaluate infrastructure performance.
        /// </summary>
        /// <param name="container">The test IoC container.</param>
        private static void PublishCommands(IComponentContext container)
        {
            var commandPublisher = container.Resolve<IPublishCommands>();
            var accounts = new List<Guid>();
            var randomizer = new Random();

            // Ensure at least 10 accounts opened prior to randomized data generation.
            for (var i = 0; i < 10; i++)
            {
                accounts.Add(GuidStrategy.NewGuid());
                commandPublisher.Publish(accounts[i], new OpenAccount((AccountType)randomizer.Next(1, 3)));
            }

            // Generate a random set of commands to exercise the underlying infrastructure.
            for (var i = 10; i < (NumberOfCommandsToPublish - 10); i++)
            {
                switch (randomizer.Next(0, 11))
                {
                    case 0:
                        var account = GuidStrategy.NewGuid();
                        commandPublisher.Publish(account, new OpenAccount((AccountType)randomizer.Next(1, 3)));
                        accounts.Add(account);
                        break;
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        commandPublisher.Publish(accounts[randomizer.Next(0, accounts.Count)], new DepositMoney((Decimal)(randomizer.Next(1, 1000000) / 100.0)));
                        break;
                    case 5:
                    case 6:
                    case 7:
                        commandPublisher.Publish(accounts[randomizer.Next(0, accounts.Count)], new DepositMoney((Decimal)(randomizer.Next(1, 500000) / 100.0)));
                        break;
                    case 8:
                    case 9:
                        var account1 = accounts[randomizer.Next(0, accounts.Count)];
                        var account2 = accounts[randomizer.Next(0, accounts.Count)];

                        while (account1 == account2)
                            account2 = accounts[randomizer.Next(0, accounts.Count)];

                        commandPublisher.Publish(account1, new SendMoneyTransfer(GuidStrategy.NewGuid(), account2, (Decimal)(randomizer.Next(1, 500000) / 100.0)));
                        break;
                    case 10:
                        commandPublisher.Publish(accounts[randomizer.Next(0, accounts.Count)], new CloseAccount());
                        break;
                }
            }
        }

        /// <summary>
        /// Wait for all commands/events to be processed before allowing process to exit.
        /// </summary>
        /// <param name="container">The test IoC container.</param>
        private static void WaitForCompletion(ILifetimeScope container)
        {
            var commandBus = container.Resolve<BlockingCollectionMessageBus<CommandEnvelope>>();
            var eventBus = container.Resolve<BlockingCollectionMessageBus<EventEnvelope>>();

            while (commandBus.Count > 0 || eventBus.Count > 0)
                Thread.Sleep(100);

            // Wait for command bus drain and shut-down.
            commandBus.WaitForDrain();
            container.Resolve<MessageReceiver<CommandEnvelope>>().Dispose();
            container.Resolve<BlockingCollectionMessageBus<CommandEnvelope>>().Dispose();

            // Wait for event bus drain and shut-down.
            eventBus.WaitForDrain();
            container.Resolve<MessageReceiver<EventEnvelope>>().Dispose();
            container.Resolve<BlockingCollectionMessageBus<EventEnvelope>>().Dispose();

            // Wait for all stores to complete any background processing.
            container.Resolve<SqlSnapshotStore>().Dispose();
            container.Resolve<SqlEventStore>().Dispose();
            container.Resolve<SqlSagaStore>().Dispose();

            // Dispose underlying IoC container.
            container.Dispose();
        }
    }
}
