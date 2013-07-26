using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Moq;
using Spark.Configuration;
using Spark.Eventing;
using Spark.EventStore;
using Spark.EventStore.Sql;
using Spark.EventStore.Sql.Dialects;
using Spark.Messaging;
using Spark.Serialization;
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

namespace Spark.Tests.EventStore.Dialects
{
    public static class UsingEventStoreWithSqlServer
    {
        public abstract class UsingInitializedEventStore : IDisposable
        {
            protected readonly IStoreEvents EventStore;
            protected readonly Mock<IStoreEventSettings> Settings;

            internal UsingInitializedEventStore()
            {
                Settings = new Mock<IStoreEventSettings>();

                Settings.Setup(mock => mock.PageSize).Returns(5);
                Settings.Setup(mock => mock.DetectDuplicateCommits).Returns(true);

                EventStore = new SqlEventStore(new BinarySerializer(), Settings.Object, new SqlServerDialect(SqlServerConnection.Name));
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
                DropExistingTable();

                Assert.DoesNotThrow(() => new SqlEventStore(new BinarySerializer(), SqlServerConnection.Name));
                Assert.True(TableExists());
            }

            [SqlServerFactAttribute]
            public void WillNotTouchTableIfExists()
            {
                Assert.DoesNotThrow(() => new SqlEventStore(new BinarySerializer(), SqlServerConnection.Name));
                Assert.DoesNotThrow(() => new SqlEventStore(new BinarySerializer(), SqlServerConnection.Name));
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

        public class WhenDisposingSnapshotStore : UsingInitializedEventStore
        {

            [SqlServerFactAttribute]
            public void CanDisposeSynchronousStore()
            {
                var settings = new Mock<IStoreEventSettings>();
                settings.Setup(mock => mock.Async).Returns(false);

                new SqlEventStore(new BinarySerializer(), settings.Object, new SqlServerDialect(SqlServerConnection.Name)).Dispose();
            }

            [SqlServerFactAttribute]
            public void CanDisposeAsynchronousStore()
            {
                var settings = new Mock<IStoreEventSettings>();
                settings.Setup(mock => mock.Async).Returns(true);

                new SqlEventStore(new BinarySerializer(), settings.Object, new SqlServerDialect(SqlServerConnection.Name)).Dispose();
            }

            [SqlServerFactAttribute]
            public void CanSafelyCallDisposeMultipleTimes()
            {
                var snapshotStore = new SqlEventStore(new BinarySerializer(), Settings.Object, new SqlServerDialect(SqlServerConnection.Name));

                snapshotStore.Dispose();

                Assert.DoesNotThrow(() => snapshotStore.Dispose());
            }
        }

        public class WhenSavingCommit : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void ThrowDuplicateCommitExceptionIfCommitAlreadyExists()
            {
                var commit1 = new Commit(Guid.NewGuid(), Guid.NewGuid(), 1, HeaderCollection.Empty, EventCollection.Empty);
                var commit2 = new Commit(commit1.CorrelationId, Guid.NewGuid(), 2, HeaderCollection.Empty, EventCollection.Empty);

                EventStore.Save(commit1);

                Assert.Throws<DuplicateCommitException>(() => EventStore.Save(commit2));
            }

            [SqlServerFactAttribute]
            public void ThrowConcurrencyExceptionIfStreamVersionExists()
            {
                var commit1 = new Commit(Guid.NewGuid(), Guid.NewGuid(), 1, HeaderCollection.Empty, EventCollection.Empty);
                var commit2 = new Commit(Guid.NewGuid(), commit1.StreamId, 1, HeaderCollection.Empty, EventCollection.Empty);

                EventStore.Save(commit1);

                Assert.Throws<ConcurrencyException>(() => EventStore.Save(commit2));
            }

            [SqlServerFactAttribute]
            public void SaveCommitIfNextVersionInStream()
            {
                var streamId = Guid.NewGuid();
                var commit1 = new Commit(Guid.NewGuid(), streamId, 1, HeaderCollection.Empty, EventCollection.Empty);
                var commit2 = new Commit(Guid.NewGuid(), streamId, 2, HeaderCollection.Empty, EventCollection.Empty);

                EventStore.Save(commit1);
                EventStore.Save(commit2);

                Assert.Equal(2, EventStore.GetStream(streamId).Count());
            }
        }

        public class WhenMarkingCommitDispatched : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void CanPageOverCommits()
            {
                var commit = new Commit(Guid.NewGuid(), Guid.NewGuid(), 1, HeaderCollection.Empty, EventCollection.Empty);

                EventStore.Save(commit);
                EventStore.MarkDispatched(commit.Id.GetValueOrDefault());

                Assert.Equal(0, EventStore.GetUndispatched().Count(c => c.Id == commit.Id));
            }
        }

        public class WhenMigratingCommit : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void CanUpdateHeaders()
            {
                var commit = new Commit(Guid.NewGuid(), Guid.NewGuid(), 1, HeaderCollection.Empty, EventCollection.Empty);

                EventStore.Save(commit);
                EventStore.Migrate(commit.Id.GetValueOrDefault(), new HeaderCollection(new Dictionary<String, String> { { "Key", "Value" } }), EventCollection.Empty);

                Assert.Equal(1, EventStore.GetStream(commit.StreamId).Single().Headers.Count);
            }

            [SqlServerFactAttribute]
            public void CanUpdateEvents()
            {
                var commit = new Commit(Guid.NewGuid(), Guid.NewGuid(), 1, HeaderCollection.Empty, EventCollection.Empty);

                EventStore.Save(commit);
                EventStore.Migrate(commit.Id.GetValueOrDefault(), HeaderCollection.Empty, new EventCollection(new[] { new FakeEvent() }));

                Assert.Equal(1, EventStore.GetStream(commit.StreamId).Single().Events.Count);
            }

            [Serializable]
            private sealed class FakeEvent : Event
            { }
        }

        public class WhenDeletingStreams : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void DeleteAllStreamCommits()
            {
                var streamId = Guid.NewGuid();

                EventStore.Save(new Commit(Guid.NewGuid(), streamId, 1, HeaderCollection.Empty, EventCollection.Empty));
                EventStore.Save(new Commit(Guid.NewGuid(), streamId, 2, HeaderCollection.Empty, EventCollection.Empty));
                EventStore.DeleteStream(streamId);

                Assert.Equal(0, EventStore.GetStream(streamId).Count());
            }

            [SqlServerFactAttribute]
            public void DoNotDeleteOtherStreams()
            {
                var streamId = Guid.NewGuid();
                var alternateStreamid = Guid.NewGuid();

                EventStore.Save(new Commit(Guid.NewGuid(), streamId, 1, HeaderCollection.Empty, EventCollection.Empty));
                EventStore.Save(new Commit(Guid.NewGuid(), alternateStreamid, 1, HeaderCollection.Empty, EventCollection.Empty));
                EventStore.DeleteStream(streamId);

                Assert.Equal(1, EventStore.GetStream(alternateStreamid).Count());
            }
        }

        public class WhenPurgingEventStore : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void DeleteAllCommits()
            {
                EventStore.Save(new Commit(Guid.NewGuid(), Guid.NewGuid(), 1, HeaderCollection.Empty, EventCollection.Empty));
                EventStore.Save(new Commit(Guid.NewGuid(), Guid.NewGuid(), 1, HeaderCollection.Empty, EventCollection.Empty));
                EventStore.Purge();

                Assert.Equal(0, EventStore.GetAll().Count());
            }
        }

        public class WhenGettingUndispatchedCommits : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void CanPageOverAllCommits()
            {
                var streamId = Guid.NewGuid();
                var startTime = DateTime.UtcNow.AddHours(-20);

                EventStore.Purge();

                for (var i = 1; i <= 10; i++)
                    EventStore.Save(new Commit(null, startTime.AddHours(i), Guid.NewGuid(), streamId, i, HeaderCollection.Empty, EventCollection.Empty));

                Assert.Equal(10, EventStore.GetUndispatched().Count());
            }
        }

        public class WhenGettingAllCommits : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void CanPageOverCommits()
            {
                var streamId = Guid.NewGuid();
                var startTime = DateTime.UtcNow.AddHours(-20);

                EventStore.Purge();

                for (var i = 1; i <= 10; i++)
                    EventStore.Save(new Commit(null, startTime.AddHours(i), Guid.NewGuid(), streamId, i, HeaderCollection.Empty, EventCollection.Empty));

                Assert.Equal(10, EventStore.GetAll().Count());
            }
        }

        public class WhenGettingCommitRange : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void UseRangeQueryOnSequenceForSkip()
            {
                var streamId = Guid.NewGuid();
                var commits = new List<Commit>();
                var startTime = DateTime.UtcNow.AddHours(-20);

                for (var i = 1; i <= 10; i++)
                    commits.Add(new Commit(null, startTime.AddHours(i), Guid.NewGuid(), streamId, i, HeaderCollection.Empty, EventCollection.Empty));

                foreach (var commit in commits)
                    EventStore.Save(commit);

                var lowerBound = commits[3].Id.GetValueOrDefault();
                var range = EventStore.GetRange(lowerBound, 4);
                Assert.Equal(lowerBound + 1, range.Min(m => m.Id.GetValueOrDefault()));
                Assert.Equal(lowerBound + 4, range.Max(m => m.Id.GetValueOrDefault()));
            }
        }

        public class WhenGettingCommitsFromVersion : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void WillReturnCommitsGreaterThanOrEqualToVersion()
            {
                var streamId = Guid.NewGuid();
                var startTime = DateTime.UtcNow.Subtract(TimeSpan.FromHours(10));
                
                EventStore.Purge();

                for (var i = 1; i <= 10; i++)
                    EventStore.Save(new Commit(null, startTime.AddHours(i), Guid.NewGuid(), streamId, i, HeaderCollection.Empty, EventCollection.Empty));

                Assert.Equal(7, EventStore.GetStream(streamId, 4).Count());
            }
        }

        public class WhenGettingStreams : UsingInitializedEventStore
        {
            [SqlServerFactAttribute]
            public void WillReturnDistinctStreamIds()
            {
                EventStore.Purge();

                for (var i = 1; i <= 5; i++)
                {
                    var streamId = Guid.NewGuid();

                    EventStore.Save(new Commit(null, SystemTime.GetTimestamp(), Guid.NewGuid(), streamId, 1, HeaderCollection.Empty, EventCollection.Empty));
                    EventStore.Save(new Commit(null, SystemTime.GetTimestamp(), Guid.NewGuid(), streamId, 2, HeaderCollection.Empty, EventCollection.Empty));
                }

                Assert.Equal(5, EventStore.GetStreams().Count());
            }
        }
    }
}
