using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Moq;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing;
using Spark.EventStore;
using Spark.Messaging;
using Spark.Resources;
using Test.Spark.Configuration;
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

namespace Test.Spark.Cqrs.Domain
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

                eventStore.Setup(mock => mock.GetStream(id, 1)).Returns(new[] { new Commit(1L, DateTime.UtcNow, Guid.NewGuid(), id, 1, HeaderCollection.Empty, new EventCollection(events)) });

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
                eventStore.Setup(mock => mock.GetStream(id, 11)).Returns(new[] { new Commit(1L, DateTime.UtcNow, Guid.NewGuid(), id, 11, HeaderCollection.Empty, new EventCollection(events)) });

                var aggregate = aggregateStore.Get(typeof(FakeAggregate), id);

                snapshotStore.Verify(mock => mock.GetSnapshot(id, Int32.MaxValue), Times.Once());

                Assert.Equal(11, aggregate.Version);
            }

            [Fact]
            public void SaveSnapshotIfLoadedCommitsGreaterThanSnapshotInterval()
            {
                var id = GuidStrategy.NewGuid();
                var commits = new List<Commit>();
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, new AggregateStoreSettings()) ;

                for (var i = 0; i < 10; i++)
                    commits.Add(new Commit(1L, DateTime.UtcNow, Guid.NewGuid(), id, 11 + i, HeaderCollection.Empty, EventCollection.Empty));

                snapshotStore.Setup(mock => mock.GetSnapshot(id, Int32.MaxValue)).Returns(new Snapshot(id, 10, new FakeAggregate(id, 10)));
                eventStore.Setup(mock => mock.GetStream(id, 11)).Returns(commits);

                var aggregate = aggregateStore.Get(typeof(FakeAggregate), id);

                snapshotStore.Verify(mock => mock.Save(It.Is<Snapshot>(s => s.Version == 20)), Times.Once());

                Assert.Equal(20, aggregate.Version);
            }

            [Fact]
            public void ThrowInvalidOperationExceptionIfAggregateStreamCorrupt()
            {
                var id = GuidStrategy.NewGuid();
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, new AggregateStoreSettings());

                snapshotStore.Setup(mock => mock.GetSnapshot(id, Int32.MaxValue)).Returns(new Snapshot(id, 10, new FakeAggregate(id, 10)));
                eventStore.Setup(mock => mock.GetStream(id, 11)).Returns(new[] { new Commit(1L, DateTime.UtcNow, Guid.NewGuid(), id, 12, HeaderCollection.Empty, EventCollection.Empty) });

                var ex = Assert.Throws<InvalidOperationException>(() => aggregateStore.Get(typeof(FakeAggregate), id));

                Assert.Equal(Exceptions.MissingAggregateCommits.FormatWith(11, 12), ex.Message);
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
            private readonly Mock<IStoreSnapshots> snapshotStore = new Mock<IStoreSnapshots>();
            private readonly Mock<IApplyEvents> aggregateUpdater = new Mock<IApplyEvents>();
            private readonly Mock<IStoreEvents> eventStore = new Mock<IStoreEvents>();

            public WhenSavingAggregate()
            {
                SystemTime.ClearOverride();
            }

            [Fact]
            public void IncrementAggregateVersionIfSuccessful()
            {
                var version = 11;
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), version);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object);

                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                    aggregateStore.Save(aggregate, context);

                Assert.Equal(version + 1, aggregate.Version);
            }

            [Fact]
            public void CaptureAggregateTypeIfFirstCommit()
            {
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 0);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object);

                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                    aggregateStore.Save(aggregate, context);

                eventStore.Verify(mock => mock.Save(It.Is<Commit>(c => c.Headers[Header.Aggregate] == typeof(FakeAggregate).GetFullNameWithAssembly())), Times.Once());
            }

            [Fact]
            public void SaveSnapshotIfRequired()
            {
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 9);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, new AggregateStoreSettings());

                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                    aggregateStore.Save(aggregate, context);

                snapshotStore.Verify(mock => mock.Save(It.Is<Snapshot>(s => s.StreamId == aggregate.Id && s.Version == 10)), Times.Once());
            }

            [Fact]
            public void IgnoreDuplicateCommits()
            {
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 8);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, new AggregateStoreSettings());

                eventStore.Setup(mock => mock.Save(It.IsAny<Commit>())).Throws<DuplicateCommitException>();

                // ReSharper disable AccessToDisposedClosure
                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                    Assert.DoesNotThrow(() => aggregateStore.Save(aggregate, context));
                // ReSharper restore AccessToDisposedClosure
            }

            [Fact]
            public void ReThrowConcurrencyExceptions()
            {
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 8);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, new AggregateStoreSettings());

                eventStore.Setup(mock => mock.Save(It.IsAny<Commit>())).Throws<ConcurrencyException>();

                // ReSharper disable AccessToDisposedClosure
                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                    Assert.Throws<ConcurrencyException>(() => aggregateStore.Save(aggregate, context));
                // ReSharper restore AccessToDisposedClosure
            }

            [Fact]
            public void ReAttemptCommitIfOtherException()
            {
                var throwException = true;
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 8);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, new AggregateStoreSettings());

                eventStore.Setup(mock => mock.Save(It.IsAny<Commit>())).Callback(() => { if (throwException) { throwException = false; throw new InvalidOperationException(); } });

                // ReSharper disable AccessToDisposedClosure
                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                    Assert.DoesNotThrow(() => aggregateStore.Save(aggregate, context));
                // ReSharper restore AccessToDisposedClosure
            }

            [Fact]
            public void ReAttemptCommitWillTimeoutEventually()
            {
                var aggregate = new FakeAggregate(GuidStrategy.NewGuid(), 8);
                var aggregateStore = new AggregateStore(aggregateUpdater.Object, snapshotStore.Object, eventStore.Object, new AggregateStoreSettings());

                eventStore.Setup(mock => mock.Save(It.IsAny<Commit>())).Throws<InvalidOperationException>();

                // ReSharper disable AccessToDisposedClosure
                using (var context = new CommandContext(GuidStrategy.NewGuid(), HeaderCollection.Empty, CommandEnvelope.Empty))
                    Assert.Throws<TimeoutException>(() => aggregateStore.Save(aggregate, context));
                // ReSharper restore AccessToDisposedClosure
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

            [UsedImplicitly]
            private class FakeEvent : Event
            { }
        }
    }
}
