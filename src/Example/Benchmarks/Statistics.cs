using System;
using System.Globalization;
using System.Text;
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
        private readonly StringBuilder lineBuilder = new StringBuilder();

        private Int64 events, totalEvents, eventsQueued, totalEventsQueued;
        private Int64 commands, conflicts, totalCommands, totalConflicts, commandsQueued, totalCommandsQueued;
        private Int64 queries, inserts, updates, deletes, totalQueries, totalInserts, totalUpdates, totalDeletes;
        private DateTime startTime, endTime;
        private Boolean disabled = true;

        /// <summary>
        /// Indicates whether or not one or more commands and/or events are still being processed.
        /// </summary>
        public Boolean Processing => commandsQueued > 0 || eventsQueued > 0;

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
            Console.WriteLine("Started @ {0:yyyy-MM-dd HH:mm:ss,ffffff}", startTime = SystemTime.Now);
            Console.WriteLine();
            Console.WriteLine("|         Commands        |          Events         |                                 Database                                |");
            Console.WriteLine("|   Processed      Queued |   Processed      Queued |  Operations     Queries     Inserts     Updates     Deletes   Conflicts |");
            Console.WriteLine("|-------------------------|-------------------------|-------------------------------------------------------------------------|");

            events = totalEvents = eventsQueued = totalEventsQueued = 0;
            commands = conflicts = totalCommands = totalConflicts = commandsQueued = totalCommandsQueued = 0;
            queries = inserts = updates = deletes = totalQueries = totalInserts = totalUpdates = totalDeletes = 0;
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            disabled = false;
        }

        /// <summary>
        /// Stop reporting captured statistics to the console.
        /// </summary>
        public void StopCapture()
        {
            disabled = true;
            endTime = SystemTime.Now;
            timer.Change(Timeout.Infinite, Timeout.Infinite);

            WriteStatisticsLine();
            WriteAverages();

            Console.WriteLine();
            Console.WriteLine("Stopped @ {0:yyyy-MM-dd HH:mm:ss,ffffff}", endTime);
            Console.WriteLine();
        }

        /// <summary>
        /// Report statistics captured since the last interval.
        /// </summary>
        private void WriteStatisticsLine()
        {
            lock (syncLock)
            {
                // Build Line
                lineBuilder.Clear();
                lineBuilder.Append("|");
                lineBuilder.Append(commands.ToString(CultureInfo.InvariantCulture).PadLeft(12));
                lineBuilder.Append(commandsQueued.ToString(CultureInfo.InvariantCulture).PadLeft(12));
                lineBuilder.Append(" |");
                lineBuilder.Append(events.ToString(CultureInfo.InvariantCulture).PadLeft(12));
                lineBuilder.Append(eventsQueued.ToString(CultureInfo.InvariantCulture).PadLeft(12));
                lineBuilder.Append(" |");
                lineBuilder.Append((queries + inserts + updates + deletes).ToString(CultureInfo.InvariantCulture).PadLeft(12));
                lineBuilder.Append(queries.ToString(CultureInfo.InvariantCulture).PadLeft(12));
                lineBuilder.Append(inserts.ToString(CultureInfo.InvariantCulture).PadLeft(12));
                lineBuilder.Append(updates.ToString(CultureInfo.InvariantCulture).PadLeft(12));
                lineBuilder.Append(deletes.ToString(CultureInfo.InvariantCulture).PadLeft(12));
                lineBuilder.Append(conflicts.ToString(CultureInfo.InvariantCulture).PadLeft(12));
                lineBuilder.Append(" |");

                // Reset Counters
                commands = 0;
                queries = 0;
                inserts = 0;
                updates = 0;
                deletes = 0;
                conflicts = 0;
                events = 0;

                totalCommandsQueued += commandsQueued;
                totalEventsQueued += eventsQueued;
            }

            Console.WriteLine(lineBuilder);
        }

        /// <summary>
        /// Report all averages.
        /// </summary>
        private void WriteAverages()
        {
            var elapsedSeconds = Math.Max(1, Convert.ToInt64(endTime.Subtract(startTime).TotalSeconds));

            Console.WriteLine("|-------------------------|-------------------------|-------------------------------------------------------------------------|");

            lineBuilder.Clear();
            lineBuilder.Append("|");
            lineBuilder.Append((totalCommands / elapsedSeconds).ToString(CultureInfo.InvariantCulture).PadLeft(12));
            lineBuilder.Append((totalCommandsQueued / elapsedSeconds).ToString(CultureInfo.InvariantCulture).PadLeft(12));
            lineBuilder.Append(" |");
            lineBuilder.Append((totalEvents / elapsedSeconds).ToString(CultureInfo.InvariantCulture).PadLeft(12));
            lineBuilder.Append((totalEventsQueued / elapsedSeconds).ToString(CultureInfo.InvariantCulture).PadLeft(12));
            lineBuilder.Append(" |");
            lineBuilder.Append(((totalQueries + totalInserts + totalUpdates + totalDeletes) / elapsedSeconds).ToString(CultureInfo.InvariantCulture).PadLeft(12));
            lineBuilder.Append((totalQueries / elapsedSeconds).ToString(CultureInfo.InvariantCulture).PadLeft(12));
            lineBuilder.Append((totalInserts / elapsedSeconds).ToString(CultureInfo.InvariantCulture).PadLeft(12));
            lineBuilder.Append((totalUpdates / elapsedSeconds).ToString(CultureInfo.InvariantCulture).PadLeft(12));
            lineBuilder.Append((totalDeletes / elapsedSeconds).ToString(CultureInfo.InvariantCulture).PadLeft(12));
            lineBuilder.Append((totalConflicts / elapsedSeconds).ToString(CultureInfo.InvariantCulture).PadLeft(12));
            lineBuilder.Append(" |");

            Console.WriteLine(lineBuilder);
        }

        /// <summary>
        /// Increment queued command count.
        /// </summary>
        public void IncrementQueuedCommands()
        {
            if (disabled) return;
            lock (syncLock)
            {
                commandsQueued++;
            }
        }

        /// <summary>
        /// Decrement queued command count and increment processed command count.
        /// </summary>
        public void DecrementQueuedCommands()
        {
            if (disabled) return;
            lock (syncLock)
            {
                commands++;
                totalCommands++;
                commandsQueued--;
            }
        }

        /// <summary>
        /// Increment queued event count.
        /// </summary>
        public void IncrementQueuedEvents()
        {
            if (disabled) return;
            lock (syncLock)
            {
                eventsQueued++;
            }
        }

        /// <summary>
        /// Decrement queued event count and increment processed event count.
        /// </summary>
        public void DecrementQueuedEvents()
        {
            if (disabled) return;
            lock (syncLock)
            {
                events++;
                totalEvents++;
                eventsQueued--;
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
    }
}
