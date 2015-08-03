using System;
using System.Collections.Generic;
using System.Data.SqlClient;

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
    /// Defines the set of transient SQL-Server errors.
    /// </summary>
    public sealed class SqlTransientErrorRegistry : DbTransientErrorRegistry<SqlException>
    {
        /// <summary>
        /// The set of transient SQL-Server error codes.
        /// </summary>
        private static readonly HashSet<Int32> TransientErrorCodes = new HashSet<Int32> { SqlErrorCode.Deadlock };

        /// <summary>
        /// Returns <value>true</value> if the <see cref="Exception"/> <paramref name="ex"/> is a transient error; otherwise returns <value>false</value>.
        /// </summary>
        /// <param name="ex">The exception to check if represents a transient error.</param>
        protected override Boolean IsTransient(SqlException ex)
        {
            return TransientErrorCodes.Contains(ex.Number);
        }
    }
}
