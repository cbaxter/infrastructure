using System;
using System.Diagnostics;
using System.Threading;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Spark.Data.SqlClient
{
    /// <summary>
    /// Provides sequential-like Guids for database b-tree friendly inserts
    /// </summary>
    /// <remarks>SQL-Server Byte Sort Order --> 3,2,1,0,5,4,7,6,9,8,15,14,13,12,11,10</remarks>
    public static class SqlSequentialGuid
    {
        private static readonly Int64 UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        private static readonly Byte[] MachineId = GetMachineId();
        private static readonly Byte[] ProcessId = GetProcessId();
        private static Int32 increment = new Random().Next();

        /// <summary>
        /// Get a byte array representation of the current machine name.
        /// </summary>
        private static Byte[] GetMachineId()
        {
            return BitConverter.GetBytes(Environment.MachineName.GetHashCode());
        }

        /// <summary>
        /// Get a byte array representation of the current process id.
        /// </summary>
        private static Byte[] GetProcessId()
        {
            using (var process = Process.GetCurrentProcess())
                return BitConverter.GetBytes(process.Id);
        }

        /// <summary>
        /// Returns a new sequential Guid.
        /// </summary>
        public static Guid NewGuid()
        {
            var sequence = Interlocked.Increment(ref increment);
            var timestamp = (DateTime.UtcNow.Ticks - UnixEpoch) / TimeSpan.TicksPerSecond;

            return new Guid(new[]
                {
                    (Byte)(sequence >> 24), 
                    (Byte)(sequence >> 16), 
                    (Byte)(sequence >> 8), 
                    (Byte)(sequence >> 0), 
                    (Byte)(timestamp >> 8), 
                    (Byte)(timestamp >> 0), 
                    (Byte)(timestamp >> 24),
                    (Byte)(timestamp >> 16),
                    ProcessId[1],
                    ProcessId[0],
                    MachineId[3],
                    MachineId[2],
                    MachineId[1],
                    MachineId[0],
                    ProcessId[3], 
                    ProcessId[2]
                });
        }
    }
}
