using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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

/* Copyright (c) 2013 Spark Software Ltd.
 * 
 * This source is subject to the GNU Lesser General Public License.
 * See: http://www.gnu.org/copyleft/lesser.html
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE. 
 */

namespace Test.Spark.Cqrs.Eventing.Sagas.Dialects
{
    public static class UsingSagaStoreWithSqlServer
    {
        public abstract class UsingInitializedSagaStore : IDisposable
        {
            protected readonly SagaContext SagaContext;
            protected readonly IStoreSagas SagaStore;
            protected readonly Guid SagaId;

            protected UsingInitializedSagaStore()
            {
                var typeLocator = new Mock<ILocateTypes>();

                typeLocator.Setup(mock => mock.GetTypes(It.IsAny<Func<Type, Boolean>>())).Returns(new[] { typeof(FakeSaga) });

                SagaId = GuidStrategy.NewGuid();
                SagaContext = new SagaContext(typeof(FakeSaga), SagaId, new FakeEvent());
                SagaStore = new SqlSagaStore(new SqlSagaStoreDialect(SqlServerConnection.Name), new BinarySerializer(), typeLocator.Object);
            }

            public void Dispose()
            {
                SagaContext.Dispose();
                SagaStore.Purge();
            }
        }

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

                Assert.DoesNotThrow(() => new SqlSagaStore(new SqlSagaStoreDialect(SqlServerConnection.Name), new BinarySerializer(), new Mock<ILocateTypes>().Object));
                Assert.True(TableExists());
            }

            [SqlServerFact]
            public void WillNotTouchTableIfExists()
            {
                Assert.DoesNotThrow(() => new SqlSagaStore(new SqlSagaStoreDialect(SqlServerConnection.Name), new BinarySerializer(), new Mock<ILocateTypes>().Object));
                Assert.DoesNotThrow(() => new SqlSagaStore(new SqlSagaStoreDialect(SqlServerConnection.Name), new BinarySerializer(), new Mock<ILocateTypes>().Object));
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

        public class WhenCreatingSaga : UsingInitializedSagaStore
        {
            [Fact]
            public void ThrowKeyNotFoundIfSagaTypeUnknown()
            {
                var sagaStore = new SqlSagaStore(new SqlSagaStoreDialect(SqlServerConnection.Name), new BinarySerializer(), new Mock<ILocateTypes>().Object);

                Assert.Throws<KeyNotFoundException>(() => sagaStore.CreateSaga(typeof(FakeSaga), GuidStrategy.NewGuid()));
            }

            [Fact]
            public void TypeIdIsMD5HashOfSagaTypeFullName()
            {
                var saga = SagaStore.CreateSaga(typeof(FakeSaga), GuidStrategy.NewGuid());

                Assert.Equal(Guid.Parse("389d0f68-cb37-1632-d79f-799bb6ffeec8"), saga.TypeId);
            }

            [Fact]
            public void CorrelationIdIsSagaId()
            {
                var sagaId = GuidStrategy.NewGuid();
                var saga = SagaStore.CreateSaga(typeof(FakeSaga), sagaId);

                Assert.Equal(sagaId, saga.CorrelationId);
            }

            [Fact]
            public void VersionAssignedZeroValue()
            {
                var saga = SagaStore.CreateSaga(typeof(FakeSaga), GuidStrategy.NewGuid());

                Assert.Equal(0, saga.Version);
            }
        }

        public class WhenSavingNewCompletedSaga : UsingInitializedSagaStore
        {
            [Fact]
            public void DoNotInsertNewSagaIfCompleted()
            {
                var saga = new FakeSaga { Version = 0, Completed = true };

                // Since we are using a regular Fact attribute rather than a SqlServerFact, any attempt to access
                // the database will throw an exception; thus if no exception thrown we did not try to save.
                Assert.DoesNotThrow(() => SagaStore.Save(saga, SagaContext));
            }
        }

        public class WhenSavingNewSaga : UsingInitializedSagaStore
        {
            [SqlServerFact]
            public void IncrementVersionIfSaveSuccessful()
            {
                var saga = new FakeSaga { TypeId = Guid.Parse("389d0f68-cb37-1632-d79f-799bb6ffeec8"), CorrelationId = SagaId, Version = 0 };
                var savedSaga = default(Saga);

                SagaStore.Save(saga, SagaContext);

                Assert.True(SagaStore.TryGetSaga(typeof(FakeSaga), SagaId, out savedSaga));
                Assert.Equal(1, saga.Version);
            }

            [SqlServerFact]
            public void ThrowConcurrencyExceptionIfSagaAlreadyExists()
            {
                var saga1 = new FakeSaga { TypeId = Guid.Parse("389d0f68-cb37-1632-d79f-799bb6ffeec8"), CorrelationId = SagaId, Version = 0 };
                var saga2 = new FakeSaga { TypeId = Guid.Parse("389d0f68-cb37-1632-d79f-799bb6ffeec8"), CorrelationId = SagaId, Version = 0 };

                SagaStore.Save(saga1, SagaContext);

                Assert.Throws<ConcurrencyException>(() => SagaStore.Save(saga2, SagaContext));
            }
        }

        public class WhenSavingExistingSaga : UsingInitializedSagaStore
        {
            [SqlServerFact]
            public void IncrementVersionIfSaveSuccessful()
            {
                var saga = new FakeSaga { TypeId = Guid.Parse("389d0f68-cb37-1632-d79f-799bb6ffeec8"), CorrelationId = SagaId, Version = 0 };
                var savedSaga = default(Saga);

                SagaStore.Save(saga, SagaContext);
                SagaStore.Save(saga, SagaContext);

                Assert.True(SagaStore.TryGetSaga(typeof(FakeSaga), SagaId, out savedSaga));
                Assert.Equal(2, saga.Version);
            }

            [SqlServerFact]
            public void ThrowConcurrencyExceptionIfSagaVersionOutOfSync()
            {
                var saga1 = new FakeSaga { TypeId = Guid.Parse("389d0f68-cb37-1632-d79f-799bb6ffeec8"), CorrelationId = SagaId, Version = 0 };
                var saga2 = new FakeSaga { TypeId = Guid.Parse("389d0f68-cb37-1632-d79f-799bb6ffeec8"), CorrelationId = SagaId, Version = 2 };

                SagaStore.Save(saga1, SagaContext);

                Assert.Throws<ConcurrencyException>(() => SagaStore.Save(saga2, SagaContext));
            }
        }

        public class WhenSavingCompletedSaga : UsingInitializedSagaStore
        {
            [SqlServerFact]
            public void RemoveSagaIfCompleted()
            {
                var saga = new FakeSaga { TypeId = Guid.Parse("389d0f68-cb37-1632-d79f-799bb6ffeec8"), CorrelationId = SagaId, Version = 0 };
                var savedSaga = default(Saga);

                SagaStore.Save(saga, SagaContext);

                saga.Completed = true;

                SagaStore.Save(saga, SagaContext);

                Assert.False(SagaStore.TryGetSaga(typeof(FakeSaga), SagaId, out savedSaga));
            }

            [SqlServerFact]
            public void ThrowConcurrencyExceptionIfSagaVersionOutOfSync()
            {
                var saga1 = new FakeSaga { TypeId = Guid.Parse("389d0f68-cb37-1632-d79f-799bb6ffeec8"), CorrelationId = SagaId, Version = 0 };
                var saga2 = new FakeSaga { TypeId = Guid.Parse("389d0f68-cb37-1632-d79f-799bb6ffeec8"), CorrelationId = SagaId, Version = 2 };

                SagaStore.Save(saga1, SagaContext);

                saga2.Completed = true;

                Assert.Throws<ConcurrencyException>(() => SagaStore.Save(saga2, SagaContext));
            }
        }

        public class WhenDisposingSnapshotStore
        {
            [SqlServerFact]
            public void CanSafelyCallDisposeMultipleTimes()
            {
                var snapshotStore = new SqlSagaStore(new SqlSagaStoreDialect(SqlServerConnection.Name), new BinarySerializer(), new Mock<ILocateTypes>().Object);

                snapshotStore.Dispose();

                Assert.DoesNotThrow(() => snapshotStore.Dispose());
            }
        }

        [Serializable]
        private class FakeSaga : Saga
        {
            protected override void Configure(SagaConfiguration saga)
            { }
        }

        public class FakeEvent : Event
        { }
    }
}
