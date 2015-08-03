using System;
using System.Data;
using System.Data.SqlClient;
using Spark.Data.SqlClient;

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

#pragma warning disable 1591
namespace Spark.Cqrs.Eventing.Sagas.Sql.Dialects
{
    public sealed class SqlSagaStoreDialect : SqlDialect, ISagaStoreDialect
    {
        public SqlSagaStoreDialect() : base("sagaStore") { }
        public SqlSagaStoreDialect(String connectionName) : base(connectionName) { }

        // ISnapshotStoreDialect
        public String GetSaga { get { return SqlServerDialectStatements.GetSaga; } }
        public String GetScheduledTimeouts { get { return SqlServerDialectStatements.GetScheduledTimeouts; } }
        public String InsertSaga { get { return SqlServerDialectStatements.InsertSaga; } }
        public String UpdateSaga { get { return SqlServerDialectStatements.UpdateSaga; } }
        public String DeleteSaga { get { return SqlServerDialectStatements.DeleteSaga; } }
        public String DeleteSagas { get { return SqlServerDialectStatements.PurgeSagas; } }
        public String EnsureSagaTableExists { get { return SqlServerDialectStatements.EnsureSagaTableExists; } }

        // Create Methods
        public IDataParameter CreateIdParameter(Guid sagaId) { return new SqlParameter("@id", SqlDbType.UniqueIdentifier) { SourceColumn = "id", Value = sagaId }; }
        public IDataParameter CreateTypeIdParameter(Guid sagaType) { return new SqlParameter("@typeId", SqlDbType.UniqueIdentifier) { SourceColumn = "typeId", Value = sagaType }; }
        public IDataParameter CreateVersionParameter(Int32 version) { return new SqlParameter("@version", SqlDbType.Int) { SourceColumn = "version", Value = version }; }
        public IDataParameter CreateTimeoutParameter(DateTime? timeout) { return new SqlParameter("@timeout", SqlDbType.DateTime2) { SourceColumn = "timeout", Value = timeout.HasValue ? (Object)timeout.Value : DBNull.Value }; }
        public IDataParameter CreateStateParameter(Byte[] state) { return new SqlParameter("@state", SqlDbType.VarBinary, Max) { SourceColumn = "state", Value = state }; }
    }
}
#pragma warning restore 1591
