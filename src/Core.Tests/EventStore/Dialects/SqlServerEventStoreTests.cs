using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.EventStore.Dialects;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Messaging;
using Spark.Infrastructure.Serialization;
using Xunit;

/* Copyright (c) 2012 Spark Software Ltd.
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

namespace Spark.Infrastructure.Tests.EventStore.Dialects
{
    public static class UsingEventStoreWithSqlServer
    {
        public abstract class UsingInitializedEventStore : IDisposable
        {
            protected readonly IStoreEvents EventStore;

            internal UsingInitializedEventStore()
            {
                EventStore = new DbEventStore(SqlServerConnection.Name, new BinarySerializer(), new SqlServerDialect(5), true, true);
                EventStore.Initialize();
            }

            public void Dispose()
            {
                EventStore.Purge();
            }
        }

        public class WhenInitializingEventStore
        {
            [SqlServerFactAttribute]
            public void WillCreateTableIfDoesNotExist()
            {
                var eventStore = new DbEventStore(SqlServerConnection.Name, new BinarySerializer());

                DropExistingTable();

                eventStore.Initialize();

                Assert.True(TableExists());
            }

            [SqlServerFactAttribute]
            public void WillNotTouchTableIfExists()
            {
                var eventStore = new DbEventStore(SqlServerConnection.Name, new BinarySerializer());

                eventStore.Initialize();
                eventStore.Initialize();

                Assert.True(TableExists());
            }

            private void DropExistingTable()
            {
                using (var connection = SqlServerConnection.Create())
                using (var command = new SqlCommand("IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Commit') DROP TABLE [dbo].[Commit];", connection))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            private Boolean TableExists()
            {
                using (var connection = SqlServerConnection.Create())
                using (var command = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Commit';", connection))
                {
                    connection.Open();

                    return Equals(command.ExecuteScalar(), 1);
                }
            }
        }

        public class WhenSavingCommit : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void ThrowDuplicateCommitExceptionIfCommitAlreadyExists()
            {
                var commit1 = new Commit(Guid.NewGuid(), 1, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty);
                var commit2 = new Commit(Guid.NewGuid(), 2, commit1.CommitId, HeaderCollection.Empty, EventCollection.Empty);

                EventStore.SaveCommit(commit1);

                Assert.Throws<DuplicateCommitException>(() => EventStore.SaveCommit(commit2));
            }

            [SqlServerFactAttribute]
            public void ThrowConcurrencyExceptionIfStreamVersionExists()
            {
                var commit1 = new Commit(Guid.NewGuid(), 1, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty);
                var commit2 = new Commit(commit1.StreamId, 1, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty);

                EventStore.SaveCommit(commit1);

                Assert.Throws<ConcurrencyException>(() => EventStore.SaveCommit(commit2));
            }

            [SqlServerFactAttribute]
            public void SaveCommitIfNextVersionInStream()
            {
                var streamId = Guid.NewGuid();
                var commit1 = new Commit(streamId, 1, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty);
                var commit2 = new Commit(streamId, 2, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty);

                EventStore.SaveCommit(commit1);
                EventStore.SaveCommit(commit2);

                Assert.Equal(2, EventStore.GetStream(streamId).Count());
            }
        }

        public class WhenMigratingCommit : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void CanUpdateHeaders()
            {
                var commit = new Commit(Guid.NewGuid(), 1, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty);

                EventStore.SaveCommit(commit);
                EventStore.Migrate(commit.CommitId, new HeaderCollection(new Dictionary<String, Object> { { "Key", 1 } }), EventCollection.Empty);

                Assert.Equal(1, EventStore.GetStream(commit.StreamId).Single().Headers.Count);
            }

            [SqlServerFactAttribute]
            public void CanUpdateEvents()
            {
                var commit = new Commit(Guid.NewGuid(), 1, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty);

                EventStore.SaveCommit(commit);
                EventStore.Migrate(commit.CommitId, HeaderCollection.Empty, new EventCollection(new[] { new FakeEvent() }));

                Assert.Equal(1, EventStore.GetStream(commit.StreamId).Single().Events.Count);
            }

            [Serializable]
            private sealed class FakeEvent : Event
            { }
        }

        public class WhenPurgingStreams : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void DeleteAllStreamCommits()
            {
                var streamId = Guid.NewGuid();

                EventStore.SaveCommit(new Commit(streamId, 1, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty));
                EventStore.SaveCommit(new Commit(streamId, 2, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty));
                EventStore.Purge(streamId);

                Assert.Equal(0, EventStore.GetStream(streamId).Count());
            }

            [SqlServerFactAttribute]
            public void DoNotDeleteOtherStreams()
            {
                var streamId = Guid.NewGuid();
                var alternateStreamid = Guid.NewGuid();

                EventStore.SaveCommit(new Commit(streamId, 1, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty));
                EventStore.SaveCommit(new Commit(alternateStreamid, 1, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty));
                EventStore.Purge(streamId);

                Assert.Equal(1, EventStore.GetStream(alternateStreamid).Count());
            }
        }

        public class WhenPurgingAllStreams : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void DeleteAllCommits()
            {
                EventStore.SaveCommit(new Commit(Guid.NewGuid(), 1, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty));
                EventStore.SaveCommit(new Commit(Guid.NewGuid(), 1, Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty));
                EventStore.Purge();

                Assert.Equal(0, EventStore.GetAll().Count());
            }
        }

        public class WhenGettingAllCommits : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void RecordsBeforeUnixEpochIgnored()
            {
                var streamId = Guid.NewGuid();
                var startTime = new DateTime(1969, 12, 31, 20, 0, 0, DateTimeKind.Utc);
                for (var i = 1; i <= 10; i++)
                    EventStore.SaveCommit(new Commit(streamId, i, startTime.AddHours(i), Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty));

                Assert.Equal(7, EventStore.GetAll().Count());
            }
        }

        public class WhenGettingCommitsFromStartTime : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void WillReturnCommitsOnOrAfterStartTime()
            {
                var streamId = Guid.NewGuid();
                var startTime = DateTime.UtcNow.Subtract(TimeSpan.FromHours(10));
                for (var i = 1; i <= 10; i++)
                    EventStore.SaveCommit(new Commit(streamId, i, startTime.AddHours(i), Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty));

                Assert.Equal(8, EventStore.GetFrom(startTime.AddHours(3)).Count());
            }
        }

        public class WhenGettingCommitsFromVersion : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void WillReturnCommitsGreaterThanOrEqualToVersion()
            {
                var streamId = Guid.NewGuid();
                var startTime = DateTime.UtcNow.Subtract(TimeSpan.FromHours(10));
                for (var i = 1; i <= 10; i++)
                    EventStore.SaveCommit(new Commit(streamId, i, startTime.AddHours(i), Guid.NewGuid(), HeaderCollection.Empty, EventCollection.Empty));

                Assert.Equal(7, EventStore.GetStreamFrom(streamId, 4).Count());
            }
        }
    }
}
