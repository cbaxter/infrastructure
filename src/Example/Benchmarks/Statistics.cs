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
        private readonly Timer timer;
        private readonly Object syncLock = new Object();
        private Int64 commands, queries, inserts, updates, deletes, conflicts, totalCommands, totalQueries, totalInserts, totalUpdates, totalDeletes, totalConflicts;
        private Boolean disabled = true;

        /// <summary>
        /// Initializes a new instance of <see cref="Statistics"/>.
        /// </summary>
        public Statistics()
        {
            timer = new Timer(_ => WriteStatisticsLine(), null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Start reporting captured statistics to the console.
        /// </summary>
        public void StartCapture()
        {
            Console.WriteLine("Started @ {0:yyyy-MM-dd HH:mm:ss,ffffff}", SystemTime.Now);
            Console.WriteLine();
            Console.WriteLine("      Commands    Operations       Queries       Inserts       Updates       Deletes     Conflicts");
            Console.WriteLine("--------------------------------------------------------------------------------------------------");

            commands = queries = inserts = updates = deletes = conflicts = totalCommands = totalQueries = totalInserts = totalUpdates = totalDeletes = totalConflicts = 0;
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            disabled = false;
        }

        /// <summary>
        /// Stop reporting captured statistics to the console.
        /// </summary>
        public void StopCapture()
        {
            var line = String.Empty;

            disabled = true;
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            WriteStatisticsLine();

            // Build Line
            Console.WriteLine("--------------------------------------------------------------------------------------------------");
            line += totalCommands.ToString(CultureInfo.InvariantCulture).PadLeft(14);
            line += (totalQueries + totalInserts + totalUpdates + totalDeletes).ToString(CultureInfo.InvariantCulture).PadLeft(14);
            line += totalQueries.ToString(CultureInfo.InvariantCulture).PadLeft(14);
            line += totalInserts.ToString(CultureInfo.InvariantCulture).PadLeft(14);
            line += totalUpdates.ToString(CultureInfo.InvariantCulture).PadLeft(14);
            line += totalDeletes.ToString(CultureInfo.InvariantCulture).PadLeft(14);
            line += totalConflicts.ToString(CultureInfo.InvariantCulture).PadLeft(14);

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
            if (disabled) return;
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
            if (disabled) return;
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
            if (disabled) return;
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
            if (disabled) return;
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
            if (disabled) return;
            lock (syncLock)
            {
                deletes++;
                totalDeletes++;
            }
        }

        /// <summary>
        /// Increment delete count.
        /// </summary>
        public void IncrementConflictCount()
        {
            if (disabled) return;
            lock (syncLock)
            {
                conflicts++;
                totalConflicts++;
            }
        }

        /// <summary>
        /// Report statistics captured since the last interval.
        /// </summary>
        private void WriteStatisticsLine()
        {
            var line = String.Empty;

            lock (syncLock)
            {
                // Build Line
                line += commands.ToString(CultureInfo.InvariantCulture).PadLeft(14);
                line += (queries + inserts + updates + deletes).ToString(CultureInfo.InvariantCulture).PadLeft(14);
                line += queries.ToString(CultureInfo.InvariantCulture).PadLeft(14);
                line += inserts.ToString(CultureInfo.InvariantCulture).PadLeft(14);
                line += updates.ToString(CultureInfo.InvariantCulture).PadLeft(14);
                line += deletes.ToString(CultureInfo.InvariantCulture).PadLeft(14);
                line += conflicts.ToString(CultureInfo.InvariantCulture).PadLeft(14);

                // Reset Counters
                commands = 0;
                queries = 0;
                inserts = 0;
                updates = 0;
                deletes = 0;
                conflicts = 0;
            }

            Console.WriteLine(line);
        }
    }
}
