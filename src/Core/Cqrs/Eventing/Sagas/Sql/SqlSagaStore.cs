using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Spark.Data;
using Spark.Logging;
using Spark.Resources;
using Spark.Serialization;

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

namespace Spark.Cqrs.Eventing.Sagas.Sql
{
    /// <summary>
    /// An RDBMS saga store.
    /// </summary>
    public sealed class SqlSagaStore : IStoreSagas
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IReadOnlyDictionary<Type, Guid> typeToGuidMap;
        private readonly IReadOnlyDictionary<Guid, Type> guidToTypeMap;
        private readonly ISerializeObjects serializer;
        private readonly ISagaStoreDialect dialect;

        private static class Column
        {
            public const Int32 Id = 0;
            public const Int32 TypeId = 1;
            public const Int32 Version = 2;
            public const Int32 Timeout = 3;
            public const Int32 State = 4;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SqlSagaStore"/>.
        /// </summary>
        /// <param name="dialect">The database dialect associated with this <see cref="SqlSagaStore"/>.</param>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> used to store binary data.</param>
        /// <param name="typeLocator">The type locator use to retrieve all known <see cref="Saga"/> types.</param>
        public SqlSagaStore(ISagaStoreDialect dialect, ISerializeObjects serializer, ILocateTypes typeLocator)
        {
            Verify.NotNull(typeLocator, "typeLocator");
            Verify.NotNull(serializer, "serializer");
            Verify.NotNull(dialect, "dialect");

            this.dialect = dialect;
            this.serializer = serializer;
            this.typeToGuidMap = GetKnownSagas(typeLocator);
            this.guidToTypeMap = typeToGuidMap.ToDictionary(item => item.Value, item => item.Key);

            Initialize();
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="CachedSagaStore"/> class.
        /// </summary>
        public void Dispose()
        { }

        /// <summary>
        /// Generate unique saga type identifiers for all locatable <see cref="Saga"/> types.
        /// </summary>
        /// <param name="typeLocator">The type locator use to retrieve all known <see cref="Saga"/> types.</param>
        private static Dictionary<Type, Guid> GetKnownSagas(ILocateTypes typeLocator)
        {
            var knownSagas = typeLocator.GetTypes(type => type.IsClass && !type.IsAbstract && type.DerivesFrom(typeof(Saga))).ToDictionary(type => type, HashType);
            var logMessage = new StringBuilder();

            logMessage.AppendLine("Discovered sagas:");
            foreach (var saga in knownSagas)
            {
                logMessage.Append("    ");
                logMessage.AppendFormat("{0} - {1}", saga.Key, saga.Value);
                logMessage.AppendLine();
            }

            Log.Debug(logMessage.ToString);

            return knownSagas;
        }

        /// <summary>
        /// Compute the MD5 hash of the specified saga <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The saga type.</param>
        private static Guid HashType(Type type)
        {
            using (var hash = new MD5CryptoServiceProvider())
                return new Guid(hash.ComputeHash(Encoding.UTF8.GetBytes(type.FullName)));
        }

        /// <summary>
        /// Initializes a new saga store.
        /// </summary>
        private void Initialize()
        {
            using (var command = dialect.CreateCommand(dialect.EnsureSagaTableExists))
            {
                Log.Trace("Initializing saga store");

                dialect.ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Get the saga type for the specified saga type identifier.
        /// </summary>
        /// <param name="typeId">The saga type identifier.</param>
        private Type GetSagaType(Guid typeId)
        {
            Type result;
            if (guidToTypeMap.TryGetValue(typeId, out result))
                return result;

            throw new KeyNotFoundException(Exceptions.UnknownSaga.FormatWith(typeId));
        }

        /// <summary>
        /// Get the MD5 hash of the specified saga <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The saga type.</param>
        private Guid GetSagaTypeId(Type type)
        {
            Guid result;

            //NOTE: Using a variable string value as a part of the saga primary key will degrade index performance significantly.
            //      Although two guids still results in a wide index, overall allows for a much denser index than the text equivalent.
            if (typeToGuidMap.TryGetValue(type, out result))
                return result;

            throw new KeyNotFoundException(Exceptions.UnknownSaga.FormatWith(type));
        }

        /// <summary>
        /// Creates a new saga instance identified by the specified <paramref name="type"/> and <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The type of saga to be retrieved.</param>
        /// <param name="id">The correlation id of the saga to be retrieved.</param>
        public Saga CreateSaga(Type type, Guid id)
        {
            if (!typeToGuidMap.ContainsKey(type))
                throw new KeyNotFoundException(Exceptions.UnknownSaga.FormatWith(type));

            return SagaActivator.CreateInstance(type, id);
        }

        /// <summary>
        /// Attempt to retrieve an existing saga instance identified by the specified <paramref name="type"/> and <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The type of saga to be retrieved.</param>
        /// <param name="id">The correlation id of the saga to be retrieved.</param>
        /// <param name="saga">The <see cref="Saga"/> instance if found; otherwise <value>null</value>.</param>
        public Boolean TryGetSaga(Type type, Guid id, out Saga saga)
        {
            Verify.NotNull(type, "type");

            using (var command = dialect.CreateCommand(dialect.GetSaga))
            {
                Log.TraceFormat("Getting saga {0} - {1}", type, id);

                command.Parameters.Add(dialect.CreateIdParameter(id));
                command.Parameters.Add(dialect.CreateTypeIdParameter(GetSagaTypeId(type)));

                saga = dialect.QuerySingle(command, CreateSaga);
            }

            return saga != null;
        }

        /// <summary>
        /// Get all scheduled saga timeouts before the specified maximum timeout.
        /// </summary>
        /// <param name="maximumTimeout">The exclusive timeout upper bound.</param>
        public IReadOnlyList<SagaTimeout> GetScheduledTimeouts(DateTime maximumTimeout)
        {
            using (var command = dialect.CreateCommand(dialect.GetScheduledTimeouts))
            {
                Log.TraceFormat("Getting saga timeouts before {0}", maximumTimeout);

                command.Parameters.Add(dialect.CreateTimeoutParameter(maximumTimeout));

                return dialect.QueryMultiple(command, CreateSagaTimeout);
            }
        }

        /// <summary>
        /// Save the specified <paramref name="context"/> changes for the given <paramref name="saga"/>.
        /// </summary>
        /// <param name="saga">The current saga version for which the context applies.</param>
        /// <param name="context">The saga context containing the saga changes to be applied.</param>
        public Saga Save(Saga saga, SagaContext context)
        {
            Verify.NotNull(saga, "saga");

            if (saga.Version == 0 && saga.Completed)
                return saga;

            if (saga.Completed)
            {
                if (saga.Version > 0)
                    DeleteSaga(saga);
            }
            else
            {
                if (saga.Version == 0)
                    InsertSaga(saga);
                else
                    UpdateSaga(saga);
            }

            return saga;
        }

        /// <summary>
        /// Insert a new saga instance.
        /// </summary>
        /// <param name="saga">The saga instance to insert.</param>
        private void InsertSaga(Saga saga)
        {
            var state = serializer.Serialize(saga);
            var typeId = GetSagaTypeId(saga.GetType());

            using (var command = dialect.CreateCommand(dialect.InsertSaga))
            {
                Log.TraceFormat("Starting new saga {0}", saga);

                command.Parameters.Add(dialect.CreateTypeIdParameter(typeId));
                command.Parameters.Add(dialect.CreateIdParameter(saga.CorrelationId));
                command.Parameters.Add(dialect.CreateTimeoutParameter(saga.Timeout));
                command.Parameters.Add(dialect.CreateStateParameter(state));

                if (dialect.ExecuteNonQuery(command) == 0)
                    throw new ConcurrencyException(Exceptions.SagaConcurrencyConflict.FormatWith(saga.GetType(), saga.CorrelationId));
            }

            saga.Version++;
        }

        /// <summary>
        /// Update an existing saga instance.
        /// </summary>
        /// <param name="saga">The saga instance to update.</param>
        private void UpdateSaga(Saga saga)
        {
            var state = serializer.Serialize(saga);
            var typeId = GetSagaTypeId(saga.GetType());

            using (var command = dialect.CreateCommand(dialect.UpdateSaga))
            {
                Log.TraceFormat("Updating existing saga {0}", saga);

                command.Parameters.Add(dialect.CreateTypeIdParameter(typeId));
                command.Parameters.Add(dialect.CreateIdParameter(saga.CorrelationId));
                command.Parameters.Add(dialect.CreateVersionParameter(saga.Version));
                command.Parameters.Add(dialect.CreateTimeoutParameter(saga.Timeout));
                command.Parameters.Add(dialect.CreateStateParameter(state));

                if (dialect.ExecuteNonQuery(command) == 0)
                    throw new ConcurrencyException(Exceptions.SagaConcurrencyConflict.FormatWith(saga.GetType(), saga.CorrelationId));
            }

            saga.Version++;
        }

        /// <summary>
        /// Delete an existing saga instance.
        /// </summary>
        /// <param name="saga">The saga instance to delete.</param>
        private void DeleteSaga(Saga saga)
        {
            var typeId = GetSagaTypeId(saga.GetType());

            using (var command = dialect.CreateCommand(dialect.DeleteSaga))
            {
                Log.TraceFormat("Completing existing saga {0}", saga);

                command.Parameters.Add(dialect.CreateTypeIdParameter(typeId));
                command.Parameters.Add(dialect.CreateIdParameter(saga.CorrelationId));
                command.Parameters.Add(dialect.CreateVersionParameter(saga.Version));

                if (dialect.ExecuteNonQuery(command) == 0)
                    throw new ConcurrencyException(Exceptions.SagaConcurrencyConflict.FormatWith(saga.GetType(), saga.CorrelationId));
            }
        }

        /// <summary>
        /// Deletes all existing sagas from the saga store.
        /// </summary>
        public void Purge()
        {
            using (var command = dialect.CreateCommand(dialect.DeleteSagas))
            {
                Log.Trace("Purging saga store");

                dialect.ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Creates a new <see cref="Saga"/>.
        /// </summary>
        /// <param name="record">The record from which to create the new <see cref="Saga"/>.</param>
        private Saga CreateSaga(IDataRecord record)
        {
            var id = record.GetGuid(Column.Id);
            var version = record.GetInt32(Column.Version);
            var timeout = record.GetNullableDateTime(Column.Timeout);
            var saga = serializer.Deserialize<Saga>(record.GetBytes(Column.State));

            saga.CorrelationId = id;
            saga.Version = version;
            saga.Timeout = timeout;

            return saga;
        }

        /// <summary>
        /// Creates a new <see cref="Saga"/>.
        /// </summary>
        /// <param name="record">The record from which to create the new <see cref="Saga"/>.</param>
        private SagaTimeout CreateSagaTimeout(IDataRecord record)
        {
            var id = record.GetGuid(Column.Id);
            var typeId = record.GetGuid(Column.TypeId);
            var timeout = record.GetDateTime(Column.Timeout);

            return new SagaTimeout(GetSagaType(typeId), id, timeout);
        }
    }
}
