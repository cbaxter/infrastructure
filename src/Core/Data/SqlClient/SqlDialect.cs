using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Spark.Resources;

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
    /// The base SQL RDBMS dialect.
    /// </summary>
    public abstract class SqlDialect : IDbDialect
    {
        private readonly String connectionString;

        /// <summary>
        /// The <see cref="DbParameter"/> size to use when using (MAX) for VARBINARY or VARCHAR fields.
        /// </summary>
        protected const Int32 Max = Int32.MaxValue;

        /// <summary>
        /// The Sql-Server databse provider factory.
        /// </summary>
        public DbProviderFactory Provider { get { return SqlClientFactory.Instance; } }

        /// <summary>
        /// The Sql-Server database connection string.
        /// </summary>
        public String ConnectionString { get { return connectionString; } }

        /// <summary>
        /// Initializes a new instance of <see cref="SqlDialect"/>.
        /// </summary>
        /// <param name="connectionName">The connection name used by this sql dialect instance.</param>
        internal SqlDialect(String connectionName)
        {
            Verify.NotNullOrWhiteSpace(connectionName, "connectionName");

            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionName];
            if(connectionStringSettings == null)
                throw new KeyNotFoundException(Exceptions.ConnectionNotFound);

            connectionString = connectionStringSettings.ConnectionString;
        }

        /// <summary>
        /// Translate the specified <see cref="DbException"/> if required.
        /// </summary>
        /// <param name="command">The <see cref="IDbCommand"/> that generated the exception.</param>
        /// <param name="ex">The exception to translate.</param>
        public virtual Exception Translate(IDbCommand command, DbException ex)
        {
            return ex;
        }
    }
}
