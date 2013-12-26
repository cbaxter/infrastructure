using System;
using System.Globalization;
using System.Threading;

namespace Spark.Example.Benchmarks
{
    /// <summary>
    /// Track basical infrastructure statistics to evaluate overall performance.
    /// </summary>
    internal class Statistics
    {
        private Int64 commands, queries, inserts, updates, deletes, totalCommands, totalQueries, totalInserts, totalUpdates, totalDelets;
        private readonly Object syncLock = new Object();
        private readonly Timer timer;

        /// <summary>
        /// Initializes a new instance of <see cref="Statistics"/>.
        /// </summary>
        public Statistics()
        {
            timer = new Timer(Elapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Start reporting captured statistics to the console.
        /// </summary>
        public void StartCapture()
        {
            Console.WriteLine("Started @ {0:yyyy-MM-dd HH:mm:ss,ffffff}", SystemTime.Now);
            Console.WriteLine();
            Console.WriteLine("      Commands       Queries       Inserts       Updates       Deletes");
            Console.WriteLine("----------------------------------------------------------------------");

            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Stop reporting captured statistics to the console.
        /// </summary>
        public void StopCapture()
        {
            var line = String.Empty;

            timer.Change(Timeout.Infinite, Timeout.Infinite);

            Console.WriteLine("----------------------------------------------------------------------");

            // Build Line
            line += totalCommands.ToString(CultureInfo.InvariantCulture).PadLeft(14);
            line += totalQueries.ToString(CultureInfo.InvariantCulture).PadLeft(14);
            line += totalInserts.ToString(CultureInfo.InvariantCulture).PadLeft(14);
            line += totalUpdates.ToString(CultureInfo.InvariantCulture).PadLeft(14);
            line += totalDelets.ToString(CultureInfo.InvariantCulture).PadLeft(14);

            Console.WriteLine(line);
            Console.WriteLine();
            Console.WriteLine("Stopped @ {0:yyyy-MM-dd HH:mm:ss,ffffff}", SystemTime.Now);
            Console.WriteLine();
        }

        /// <summary>
        /// Increment command count.
        /// </summary>
        public void IncrementCommandCount()
        {
            lock (syncLock)
            {
                commands++;
                totalCommands++;
            }
        }

        /// <summary>
        /// Increment query count.
        /// </summary>
        public void IncrementQueryCount()
        {
            lock (syncLock)
            {
                queries++;
                totalQueries++;
            }
        }

        /// <summary>
        /// Increment insert count.
        /// </summary>
        public void IncrementInsertCount()
        {
            lock (syncLock)
            {
                inserts++;
                totalInserts++;
            }
        }

        /// <summary>
        /// Increment update count.
        /// </summary>
        public void IncrementUpdateCount()
        {
            lock (syncLock)
            {
                updates++;
                totalUpdates++;
            }
        }

        /// <summary>
        /// Increment delete count.
        /// </summary>
        public void IncrementDeleteCount()
        {
            lock (syncLock)
            {
                deletes++;
                totalDelets++;
            }
        }

        /// <summary>
        /// Report statistics captured since the last interval.
        /// </summary>
        /// <param name="state">The state.</param>
        private void Elapsed(Object state)
        {
            var line = String.Empty;

            lock (syncLock)
            {
                // Build Line
                line += commands.ToString(CultureInfo.InvariantCulture).PadLeft(14);
                line += queries.ToString(CultureInfo.InvariantCulture).PadLeft(14);
                line += inserts.ToString(CultureInfo.InvariantCulture).PadLeft(14);
                line += updates.ToString(CultureInfo.InvariantCulture).PadLeft(14);
                line += deletes.ToString(CultureInfo.InvariantCulture).PadLeft(14);

                // Reset Counters
                commands = 0;
                queries = 0;
                inserts = 0;
                updates = 0;
                deletes = 0;
            }

            Console.WriteLine(line);
        }
    }
}
