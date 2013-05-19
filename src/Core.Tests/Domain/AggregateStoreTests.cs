using System;
using JetBrains.Annotations;
using Moq;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Configuration;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Messaging;
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

namespace Spark.Infrastructure.Tests.Domain
{
    public static class UsingAggregateStore
    {
        public class WhenDisposing
        {
            [Fact]
            public void CanCallDisposeMoreThanOnce()
            {
                var aggregateStore = new AggregateStore(new Mock<IApplyEvents>().Object, new Mock<IStoreSnapshots>().Object, new Mock<IStoreEvents>().Object);

                aggregateStore.Dispose();

                Assert.DoesNotThrow(() => aggregateStore.Dispose());
            }
        }

        public class WhenGettingAggregate
        {
            private readonly Mock<IStoreSnapshots> snapshotStore = new Mock<IStoreSnapshots>();
            private readonly Mock<IApplyEvents> aggregateUpdater = new Mock<IApplyEvents>();
            private readonly Mock<IStoreEvents> eventStore = new Mock<IStoreEvents>();

            [Fact]
            public void CreateNewAggregateIfNoExistingEventStream()
            {
                var id = GuidStrategy.NewGuid();
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object);
                var aggregate = aggregateStore.Get(typeof(FakeAggregate), id);

                Assert.Equal(0, aggregate.Version);
            }

            [Fact]
            public void RebuildAggregateIfExistingEventStream()
            {
                var id = GuidStrategy.NewGuid();
                var events = new Event[] { new FakeEvent(), new FakeEvent() };
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object);

                eventStore.Setup(mock => mock.GetStream(id, 0)).Returns(new[] { new Commit(Guid.NewGuid(), 1L, DateTime.UtcNow, id, 1, HeaderCollection.Empty, new EventCollection(events)) });

                var aggregate = aggregateStore.Get(typeof(FakeAggregate), id);

                Assert.Equal(1, aggregate.Version);
            }

            [Fact]
            public void UseSnapshotIfAvailable()
            {
                var id = GuidStrategy.NewGuid();
                var snapshot = new FakeAggregate(id, 10);
                var events = new Event[] { new FakeEvent(), new FakeEvent() };
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object);

                snapshotStore.Setup(mock => mock.GetSnapshot(id, Int32.MaxValue)).Returns(new Snapshot(id, 10, snapshot));
                eventStore.Setup(mock => mock.GetStream(id, 10)).Returns(new[] { new Commit(Guid.NewGuid(), 1L, DateTime.UtcNow, id, 11, HeaderCollection.Empty, new EventCollection(events)) });

                var aggregate = aggregateStore.Get(typeof(FakeAggregate), id);

                snapshotStore.Verify(mock => mock.GetSnapshot(id, Int32.MaxValue), Times.Once());

                Assert.Equal(11, aggregate.Version);
            }

            private class FakeAggregate : Aggregate
            {
                [UsedImplicitly]
                public FakeAggregate()
                { }

                public FakeAggregate(Guid id, Int32 version)
                {
                    Id = id;
                    Version = version;
                }
            }

            private class FakeEvent : Event
            { }
        }

        public class WhenSavingAggregate
        {
            private readonly Mock<IStoreAggregateSettings> settings = new Mock<IStoreAggregateSettings>();
            private readonly Mock<IStoreSnapshots> snapshotStore = new Mock<IStoreSnapshots>();
            private readonly Mock<IApplyEvents> aggregateUpdater = new Mock<IApplyEvents>();
            private readonly Mock<IStoreEvents> eventStore = new Mock<IStoreEvents>();

            public WhenSavingAggregate()
            {
                SystemTime.ClearOverride();

                settings.Setup(mock => mock.SnapshotInterval).Returns(10);
                settings.Setup(mock => mock.SaveRetryTimeout).Returns(TimeSpan.FromMilliseconds(100));
            }

            [Fact]
            public void IncrementAggregateVersionIfSuccessful()
            {
                var version = 11;
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), version);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object);

                aggregateStore.Save(aggregate, new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty));

                Assert.Equal(version + 1, aggregate.Version);
            }

            [Fact]
            public void CaptureAggregateTypeIfFirstCommit()
            {
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 0);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object);

                aggregateStore.Save(aggregate, new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty));

                eventStore.Verify(mock => mock.Save(It.Is<Commit>(c => (Type)c.Headers[Header.Aggregate] == typeof(FakeAggregate))), Times.Once());
            }

            [Fact]
            public void ReplaceSnapshotIfRequired()
            {
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 9);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, settings.Object);

                aggregateStore.Save(aggregate, new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty));

                snapshotStore.Verify(mock => mock.ReplaceSnapshot(It.Is<Snapshot>(s => s.StreamId == aggregate.Id && s.Version == 10)), Times.Once());
            }

            [Fact]
            public void IgnoreDuplicateCommits()
            {
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 8);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, settings.Object);

                eventStore.Setup(mock => mock.Save(It.IsAny<Commit>())).Throws<DuplicateCommitException>();

                Assert.DoesNotThrow(() => aggregateStore.Save(aggregate, new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty)));
            }

            [Fact]
            public void ReThrowConcurrencyExceptions()
            {
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 8);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, settings.Object);

                eventStore.Setup(mock => mock.Save(It.IsAny<Commit>())).Throws<ConcurrencyException>();

                Assert.Throws<ConcurrencyException>(() => aggregateStore.Save(aggregate, new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty)));
            }

            [Fact]
            public void ReAttemptCommitIfOtherException()
            {
                var throwException = true;
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 8);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, settings.Object);

                eventStore.Setup(mock => mock.Save(It.IsAny<Commit>())).Callback(() => { if (throwException) { throwException = false; throw new InvalidOperationException(); } });

                Assert.DoesNotThrow(() => aggregateStore.Save(aggregate, new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty)));
            }

            [Fact]
            public void ReAttemptCommitWillTimeoutEventually()
            {
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 8);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, settings.Object);

                eventStore.Setup(mock => mock.Save(It.IsAny<Commit>())).Throws<InvalidOperationException>();

                Assert.Throws<TimeoutException>(() => aggregateStore.Save(aggregate, new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty)));
            }

            private class FakeAggregate : Aggregate
            {
                [UsedImplicitly]
                public FakeAggregate()
                { }

                public FakeAggregate(Guid id, Int32 version)
                {
                    Id = id;
                    Version = version;
                }
            }

            private class FakeEvent : Event
            { }
        }
    }
}
