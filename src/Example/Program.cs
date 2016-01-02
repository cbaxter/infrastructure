using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Autofac;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Cqrs.Eventing.Sagas.Sql;
using Spark.Data.SqlClient;
using Spark.EventStore;
using Spark.EventStore.Sql;
using Spark.Example.Benchmarks;
using Spark.Example.Domain;
using Spark.Example.Domain.Commands;
using Spark.Example.Modules;
using Spark.Messaging;
using Spark.Messaging.Msmq;
using IContainer = Autofac.IContainer;

namespace Spark.Example
{
    /// <summary>
    /// Basic test program used to evaluate infrastructure performance.
    /// </summary>
    internal static class Program
    {
        private static readonly IReadOnlyDictionary<String, MessageBusType> KnownMessageBusTypes;

        static Program()
        {
            KnownMessageBusTypes = typeof(MessageBusType).GetFields().Where(field => field.GetCustomAttributes(inherit: false).OfType<DescriptionAttribute>().Any()).ToDictionary(
                field => field.GetCustomAttributes(inherit: false).OfType<DescriptionAttribute>().Single().Description,
                field => (MessageBusType)field.GetValue(null), StringComparer.InvariantCultureIgnoreCase
            );
        }

        /// <summary>
        /// The main entry point for the test program.
        /// </summary>
        internal static void Main(String[] args)
        {
            var numberOfIterations = GetNumberOfIterations(args);
            var numberOfCommands = GetNumberOfCommands(args);

            CommandingModule.MessageBusType = GetCommandBusType(args);
            EventingModule.MessageBusType = GetEventBusType(args);

            for (var i = 0; i < numberOfIterations; i++)
            {
                var container = Initialize();

                Purge(container);
                PublishCommands(container, numberOfCommands);
                WaitForCompletion(container);
            }

            if (Debugger.IsAttached)
            {
                Console.Write("Press any key to continue . . . ");
                Console.Read();
            }
        }

        /// <summary>
        /// Get the number of test iterations to complete (i.e., testing setup/teardown).
        /// </summary>
        /// <param name="args">The command line arguments</param>
        private static Int32 GetNumberOfIterations(String[] args)
        {
            Int32 result;
            String value;

            if (args.Length > 0 && Int32.TryParse(args[0] ?? String.Empty, out result)) return result;
            do
            {
                Console.Write("Number of iterations (default = 1): ");
            } while (!Int32.TryParse((value = Console.ReadLine()).IsNullOrWhiteSpace() ? "1" : value, out result));

            return result;
        }

        /// <summary>
        /// Get the number of commands to publish/process.
        /// </summary>
        /// <param name="args">The command line arguments</param>
        private static Int32 GetNumberOfCommands(String[] args)
        {
            Int32 result;
            String value;

            if (args.Length > 1 && Int32.TryParse(args[1] ?? String.Empty, out result)) return result;
            do
            {
                Console.Write("Number of commands (default = 20,000): ");
            } while (!Int32.TryParse((value = Console.ReadLine()).IsNullOrWhiteSpace() ? "20000" : value, out result));

            return result;
        }

        /// <summary>
        /// Get the command bus type to be registered.
        /// </summary>
        /// <param name="args">The command line arguments</param>
        private static MessageBusType GetCommandBusType(String[] args)
        {
            MessageBusType result;

            if (args.Length > 2 && TryGetMessageBusType(args[2], out result)) return result;
            do
            {
                Console.Write("Command Bus [1 = Inline, 2 = Optimistic, 3 = MSMQ] (default = MSMQ): ");
            } while (!TryGetMessageBusType(Console.ReadLine() ?? String.Empty, out result));

            return result;
        }

        /// <summary>
        /// Get the event bus type to be registered.
        /// </summary>
        /// <param name="args">The command line arguments</param>
        private static MessageBusType GetEventBusType(String[] args)
        {
            MessageBusType result;

            if (args.Length > 3 && TryGetMessageBusType(args[3], out result)) return result;
            do
            {
                Console.Write("Event Bus [1 = Inline, 2 = Optimistic, 3 = MSMQ] (default = MSMQ): ");
            } while (!TryGetMessageBusType(Console.ReadLine() ?? String.Empty, out result));

            return result;
        }

        /// <summary>
        /// Gets a message bus type based on the specified string <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to convert to an message bus type (i.e., name, value or unique description).</param>
        /// <param name="result">The message bus type.</param>
        public static Boolean TryGetMessageBusType(String value, out MessageBusType result)
        {
            Int32 parsedValue;
            Object boxedValue = Int32.TryParse(value, out parsedValue) ? parsedValue : (Object)value;

            if (value.IsNullOrWhiteSpace())
            {
                result = MessageBusType.MicrosoftMessageQueuing;
                return true;
            }

            if (Enum.IsDefined(typeof(MessageBusType), boxedValue))
            {
                result = (MessageBusType)Enum.ToObject(typeof(MessageBusType), boxedValue);
                return true;
            }

            return KnownMessageBusTypes.TryGetValue(value, out result);
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
            var statistics = container.Resolve<Statistics>();

            Console.WriteLine();

            Console.WriteLine("Purging snapshot store...");
            snapshotStore.Purge();

            Console.WriteLine("Purging event store...");
            eventStore.Purge();

            Console.WriteLine("Purging saga store...");
            sagaStore.Purge();

            Console.WriteLine();

            statistics.StartCapture();
        }

        /// <summary>
        /// Publish a set of pre-defined test commands to evaluate infrastructure performance.
        /// </summary>
        /// <param name="container">The test IoC container.</param>
        /// <param name="numberOfCommands">The number of commands to publish.</param>
        private static void PublishCommands(IComponentContext container, Int32 numberOfCommands)
        {
            var commandPublisher = container.Resolve<IPublishCommands>();
            var accounts = new List<Guid>();
            var randomizer = new Random();

            // Ensure at least 10 accounts opened prior to randomized data generation.
            for (var i = 0; i < Math.Min(numberOfCommands, 10); i++)
            {
                accounts.Add(GuidStrategy.NewGuid());
                commandPublisher.Publish(accounts[i], new OpenAccount((AccountType)randomizer.Next(1, 3)));
            }

            // Generate a random set of commands to exercise the underlying infrastructure.
            for (var i = 10; i < numberOfCommands - 10; i++)
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
            var statistics = container.Resolve<Statistics>();

            // Wait for statistics to stop capturing.
            while (statistics.Processing)
                Thread.Sleep(100);

            //Wait for all commands to be processed.
            WaitForCommandBusDrain(container);

            //Wait for all events to be processed.
            WaitForEventBusDrain(container);

            // Stop statistics collection.
            statistics.StopCapture();

            // Wait for all stores to complete any background processing.
            container.Resolve<SqlSnapshotStore>().Dispose();
            container.Resolve<SqlEventStore>().Dispose();
            container.Resolve<SqlSagaStore>().Dispose();

            // Dispose underlying IoC container.
            container.Dispose();
        }

        /// <summary>
        /// Wait for all commands to be processed before continuing.
        /// </summary>
        /// <param name="container">The test IoC container.</param>
        private static void WaitForCommandBusDrain(ILifetimeScope container)
        {
            switch (CommandingModule.MessageBusType)
            {
                case MessageBusType.Inline:
                    break;
                case MessageBusType.Optimistic:
                    container.Resolve<OptimisticMessageSender<CommandEnvelope>>().Dispose();
                    break;
                case MessageBusType.MicrosoftMessageQueuing:
                    container.Resolve<MessageReceiver<CommandEnvelope>>().Dispose();
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Wait for all events to be processed before continuing.
        /// </summary>
        /// <param name="container">The test IoC container.</param>
        private static void WaitForEventBusDrain(ILifetimeScope container)
        {
            switch (EventingModule.MessageBusType)
            {
                case MessageBusType.Inline:
                    break;
                case MessageBusType.Optimistic:
                    container.Resolve<OptimisticMessageSender<EventEnvelope>>().Dispose();
                    break;
                case MessageBusType.MicrosoftMessageQueuing:
                    container.Resolve<MessageReceiver<EventEnvelope>>().Dispose();
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
