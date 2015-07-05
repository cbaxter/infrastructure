using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Moq;
using Spark;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Cqrs.Eventing.Sagas.Sql;
using Spark.Cqrs.Eventing.Sagas.Sql.Dialects;
using Spark.Data;
using Spark.Serialization;
using Test.Spark.Data;
using Xunit;

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

namespace Test.Spark.Cqrs.Eventing.Sagas.Dialects
{
    namespace UsingSagaStoreWithSqlServer
    {
        public abstract class UsingInitializedSagaStore : IDisposable
        {
            protected readonly SqlSagaStoreDialect Dialect = new SqlSagaStoreDialect(SqlServerConnection.Name);
            protected readonly Mock<ILocateTypes> TypeLocator = new Mock<ILocateTypes>();
            protected readonly ISerializeObjects Serializer = new BinarySerializer();
            protected readonly SagaContext SagaContext;
            protected readonly IStoreSagas SagaStore;
            protected readonly Guid SagaId;

            protected UsingInitializedSagaStore()
            {
                SagaId = GuidStrategy.NewGuid();
                SagaContext = new SagaContext(typeof(FakeSaga), SagaId, new FakeEvent());
                TypeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(FakeSaga) });
                SagaStore = new SqlSagaStore(Dialect, Serializer, TypeLocator.Object);
                SagaStore.Purge();
            }

            public void Dispose()
            {
                SagaContext.Dispose();
                SagaStore.Purge();
            }
        }

        [Collection(SqlServerConnection.Name)]
        public class WhenInitializingSagaStore
        {
            public void DialectCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new SqlSagaStore(null, new BinarySerializer(), new Mock<ILocateTypes>().Object));

                Assert.Equal("dialect", ex.ParamName);
            }

            public void SerializerCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new SqlSagaStore(new SqlSagaStoreDialect(), null, new Mock<ILocateTypes>().Object));

                Assert.Equal("serializer", ex.ParamName);
            }

            public void TypeLocatorCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new SqlSagaStore(new SqlSagaStoreDialect(), new BinarySerializer(), null));

                Assert.Equal("typeLocator", ex.ParamName);
            }

            [SqlServerFact]
            public void WillCreateTableIfDoesNotExist()
            {
                DropExistingTable();

                Assert.NotNull(new SqlSagaStore(new SqlSagaStoreDialect(SqlServerConnection.Name), new BinarySerializer(), new Mock<ILocateTypes>().Object));
                Assert.True(TableExists());
            }

            [SqlServerFact]
            public void WillNotTouchTableIfExists()
            {
                Assert.NotNull(new SqlSagaStore(new SqlSagaStoreDialect(SqlServerConnection.Name), new BinarySerializer(), new Mock<ILocateTypes>().Object));
                Assert.NotNull(new SqlSagaStore(new SqlSagaStoreDialect(SqlServerConnection.Name), new BinarySerializer(), new Mock<ILocateTypes>().Object));
                Assert.True(TableExists());
            }

            private void DropExistingTable()
            {
                using (var connection = SqlServerConnection.Create())
                using (var command = new SqlCommand("IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Saga') DROP TABLE [dbo].[Saga];", connection))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            private Boolean TableExists()
            {
                using (var connection = SqlServerConnection.Create())
                using (var command = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Saga';", connection))
                {
                    connection.Open();

                    return Equals(command.ExecuteScalar(), 1);
                }
            }
        }

        [Collection(SqlServerConnection.Name)]
        public class WhenCreatingSaga : UsingInitializedSagaStore
        {
            [SqlServerFact]
            public void ThrowKeyNotFoundIfSagaTypeUnknown()
            {
                var sagaStore = new SqlSagaStore(new SqlSagaStoreDialect(SqlServerConnection.Name), new BinarySerializer(), new Mock<ILocateTypes>().Object);

                Assert.Throws<KeyNotFoundException>(() => sagaStore.CreateSaga(typeof(FakeSaga), GuidStrategy.NewGuid()));
            }

            [SqlServerFact]
            public void CorrelationIdIsSagaId()
            {
                var sagaId = GuidStrategy.NewGuid();
                var saga = SagaStore.CreateSaga(typeof(FakeSaga), sagaId);

                Assert.Equal(sagaId, saga.CorrelationId);
            }

            [SqlServerFact]
            public void VersionAssignedZeroValue()
            {
                var saga = SagaStore.CreateSaga(typeof(FakeSaga), GuidStrategy.NewGuid());

                Assert.Equal(0, saga.Version);
            }
        }

        [Collection(SqlServerConnection.Name)]
        public class WhenGettingScheduledTimeouts : UsingInitializedSagaStore
        {
            [SqlServerFact]
            public void TimeoutUpperBoundIsExclusive()
            {
                var timeout = DateTime.Now.AddMinutes(20);
                var saga = new FakeSaga { Version = 0, Timeout = timeout };

                SagaStore.Save(saga, SagaContext);

                Assert.Equal(0, SagaStore.GetScheduledTimeouts(timeout).Count);
            }

            [SqlServerFact]
            public void TimeoutContainsAllNonStateData()
            {
                var timeout = DateTime.Now.AddMinutes(20);
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid(), Version = 0, Timeout = timeout };

                SagaStore.Save(saga, SagaContext);

                var sagaTimeout = SagaStore.GetScheduledTimeouts(timeout.AddMinutes(1)).Single();
                Assert.Equal(saga.CorrelationId, sagaTimeout.SagaId);
                Assert.Equal(saga.GetType(), sagaTimeout.SagaType);
                Assert.Equal(saga.Timeout, sagaTimeout.Timeout);
            }

            [SqlServerFact]
            public void ThrowKeyNotFoundIfSagaTypeUnknown()
            {
                var timeout = DateTime.Now.AddMinutes(20);
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid(), Version = 0, Timeout = timeout };
                var state = Serializer.Serialize(saga);

                using (var command = Dialect.CreateCommand(Dialect.InsertSaga))
                {
                    command.Parameters.Add(Dialect.CreateTypeIdParameter(Guid.NewGuid()));
                    command.Parameters.Add(Dialect.CreateIdParameter(saga.CorrelationId));
                    command.Parameters.Add(Dialect.CreateTimeoutParameter(timeout));
                    command.Parameters.Add(Dialect.CreateStateParameter(state));

                    Dialect.ExecuteNonQuery(command);
                }

                Assert.Throws<KeyNotFoundException>(() => SagaStore.GetScheduledTimeouts(timeout.AddMinutes(1)));
            }
        }

        [Collection(SqlServerConnection.Name)]
        public class WhenSavingNewCompletedSaga : UsingInitializedSagaStore
        {
            [SqlServerFact]
            public void DoNotInsertNewSagaIfCompleted()
            {
                var saga = new FakeSaga { Version = 0, Completed = true };
                var result = default(Saga);

                SagaStore.Save(saga, SagaContext);

                Assert.False(SagaStore.TryGetSaga(typeof(FakeSaga), saga.CorrelationId, out result));
            }
        }

        [Collection(SqlServerConnection.Name)]
        public class WhenSavingNewSaga : UsingInitializedSagaStore
        {
            [SqlServerFact]
            public void IncrementVersionIfSaveSuccessful()
            {
                var saga = new FakeSaga { CorrelationId = SagaId, Version = 0 };
                var savedSaga = default(Saga);

                SagaStore.Save(saga, SagaContext);

                Assert.True(SagaStore.TryGetSaga(typeof(FakeSaga), SagaId, out savedSaga));
                Assert.Equal(1, saga.Version);
            }

            [SqlServerFact]
            public void ThrowConcurrencyExceptionIfSagaAlreadyExists()
            {
                var saga1 = new FakeSaga { CorrelationId = SagaId, Version = 0 };
                var saga2 = new FakeSaga { CorrelationId = SagaId, Version = 0 };

                SagaStore.Save(saga1, SagaContext);

                Assert.Throws<ConcurrencyException>(() => SagaStore.Save(saga2, SagaContext));
            }
        }

        [Collection(SqlServerConnection.Name)]
        public class WhenSavingExistingSaga : UsingInitializedSagaStore
        {
            [SqlServerFact]
            public void IncrementVersionIfSaveSuccessful()
            {
                var saga = new FakeSaga { CorrelationId = SagaId, Version = 0 };
                var savedSaga = default(Saga);

                SagaStore.Save(saga, SagaContext);
                SagaStore.Save(saga, SagaContext);

                Assert.True(SagaStore.TryGetSaga(typeof(FakeSaga), SagaId, out savedSaga));
                Assert.Equal(2, saga.Version);
            }

            [SqlServerFact]
            public void ThrowConcurrencyExceptionIfSagaVersionOutOfSync()
            {
                var saga1 = new FakeSaga { CorrelationId = SagaId, Version = 0 };
                var saga2 = new FakeSaga { CorrelationId = SagaId, Version = 2 };

                SagaStore.Save(saga1, SagaContext);

                Assert.Throws<ConcurrencyException>(() => SagaStore.Save(saga2, SagaContext));
            }
        }

        [Collection(SqlServerConnection.Name)]
        public class WhenSavingCompletedSaga : UsingInitializedSagaStore
        {
            [SqlServerFact]
            public void RemoveSagaIfCompleted()
            {
                var saga = new FakeSaga { CorrelationId = SagaId, Version = 0 };
                var savedSaga = default(Saga);

                SagaStore.Save(saga, SagaContext);

                saga.Completed = true;

                SagaStore.Save(saga, SagaContext);

                Assert.False(SagaStore.TryGetSaga(typeof(FakeSaga), SagaId, out savedSaga));
            }

            [SqlServerFact]
            public void ThrowConcurrencyExceptionIfSagaVersionOutOfSync()
            {
                var saga1 = new FakeSaga { CorrelationId = SagaId, Version = 0 };
                var saga2 = new FakeSaga { CorrelationId = SagaId, Version = 2 };

                SagaStore.Save(saga1, SagaContext);

                saga2.Completed = true;

                Assert.Throws<ConcurrencyException>(() => SagaStore.Save(saga2, SagaContext));
            }
        }

        [Collection(SqlServerConnection.Name)]
        public class WhenDisposingSnapshotStore
        {
            [SqlServerFact]
            public void CanSafelyCallDisposeMultipleTimes()
            {
                using (var snapshotStore = new SqlSagaStore(new SqlSagaStoreDialect(SqlServerConnection.Name), new BinarySerializer(), new Mock<ILocateTypes>().Object))
                {
                    snapshotStore.Dispose();
                    snapshotStore.Dispose();
                }
            }
        }

        [Serializable]
        internal class FakeSaga : Saga
        {
            protected override void Configure(SagaConfiguration saga)
            { }
        }

        internal class FakeEvent : Event
        { }
    }
}
