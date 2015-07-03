using System;
using System.Collections.Generic;
using Moq;
using Spark;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Messaging;
using Spark.Threading;
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

namespace Test.Spark.Cqrs.Eventing.Sagas
{
    namespace UsingTimeoutDispatcher
    {
        public class WhenDisposing
        {
            [Fact]
            public void CanSafelyDisposeMoreThanOnce()
            {
                using (var dispatcher = new TimeoutDispatcher(new Lazy<IStoreSagas>(() => new Mock<IStoreSagas>().Object), new Lazy<IPublishEvents>(() => new Mock<IPublishEvents>().Object)))
                {
                    dispatcher.Dispose();
                    dispatcher.Dispose();
                }
            }
        }

        public class OnPostSave : IDisposable
        {
            private readonly Mock<IStoreSagas> sagaStore;
            private readonly Mock<IPublishEvents> eventPublisher;
            private readonly TimeoutDispatcher timeoutDispatcher;
            private readonly SagaTimeout sagaTimeout;
            private readonly DateTime now;
            private FakeTimer timer;

            public OnPostSave()
            {
                now = DateTime.UtcNow;
                sagaStore = new Mock<IStoreSagas>();
                eventPublisher = new Mock<IPublishEvents>();
                timeoutDispatcher = new TimeoutDispatcher(new Lazy<IStoreSagas>(() => sagaStore.Object), new Lazy<IPublishEvents>(() => eventPublisher.Object), callback => timer = new FakeTimer(callback));
                sagaTimeout = new SagaTimeout(typeof(FakeSaga), GuidStrategy.NewGuid(), now.AddMinutes(5));

                SystemTime.OverrideWith(() => now);

                sagaStore.Setup(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>())).Returns(new[] { sagaTimeout });

                timer.InvokeCallback();
                timer.Reset();
            }

            public void Dispose()
            {
                SystemTime.ClearOverride();
            }

            [Fact]
            public void DoNotScheduleTimeoutIfTimeoutHasNotChanged()
            {
                var saga = new FakeSaga { Timeout = null };

                using (var context = new SagaContext(typeof(FakeSaga), GuidStrategy.NewGuid(), new FakeEvent()))
                    timeoutDispatcher.PostSave(saga, context, null);

                Assert.False(timer.Changed);
            }

            [Fact]
            public void DoNotScheduleTimeoutIfSagaNull()
            {
                using (var context = new SagaContext(typeof(FakeSaga), GuidStrategy.NewGuid(), new FakeEvent()))
                {
                    context.TimeoutChanged = true;
                    timeoutDispatcher.PostSave(null, context, null);
                }

                Assert.False(timer.Changed);
            }

            [Fact]
            public void DoNotScheduleTimeoutIfErrorNotNull()
            {
                var saga = new FakeSaga { Timeout = null };

                using (var context = new SagaContext(typeof(FakeSaga), GuidStrategy.NewGuid(), new FakeEvent()))
                {
                    context.TimeoutChanged = true;
                    timeoutDispatcher.PostSave(saga, context, new Exception());
                }

                Assert.False(timer.Changed);
            }

            [Fact]
            public void ClearTimeoutIfSagaCompleted()
            {
                var saga = new FakeSaga { CorrelationId = sagaTimeout.SagaId, Timeout = sagaTimeout.Timeout, Completed = true };
                var cachedItems = timeoutDispatcher.TimeoutCache.Count - 1;

                using (var context = new SagaContext(sagaTimeout.SagaType, sagaTimeout.SagaId, new FakeEvent()))
                {
                    context.TimeoutChanged = true;
                    timeoutDispatcher.PostSave(saga, context, null);
                }

                Assert.Equal(cachedItems, timeoutDispatcher.TimeoutCache.Count);
            }

            [Fact]
            public void UpdateTimerIfClearedTimeoutWasNextTimeout()
            {
                var saga = new FakeSaga { CorrelationId = sagaTimeout.SagaId, Timeout = null };

                using (var context = new SagaContext(sagaTimeout.SagaType, sagaTimeout.SagaId, new FakeEvent()))
                {
                    context.TimeoutChanged = true;
                    timeoutDispatcher.PostSave(saga, context, null);
                }

                Assert.True(timer.Changed);
            }

            [Fact]
            public void DoNotUpdateTimerIfClearedTimeoutWasNotNextTimeout()
            {
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid(), Timeout = null };

                using (var context = new SagaContext(sagaTimeout.SagaType, saga.CorrelationId, new FakeEvent()))
                {
                    context.TimeoutChanged = true;
                    timeoutDispatcher.PostSave(saga, context, null);
                }

                Assert.False(timer.Changed);
            }

            [Fact]
            public void ClearTimeoutIfTimeoutChangedAndHasNoValue()
            {
                var saga = new FakeSaga { CorrelationId = sagaTimeout.SagaId, Timeout = null };
                var cachedItems = timeoutDispatcher.TimeoutCache.Count - 1;

                using (var context = new SagaContext(sagaTimeout.SagaType, sagaTimeout.SagaId, new FakeEvent()))
                {
                    context.TimeoutChanged = true;
                    timeoutDispatcher.PostSave(saga, context, null);
                }

                Assert.Equal(cachedItems, timeoutDispatcher.TimeoutCache.Count);
            }

            [Fact]
            public void ScheduleTimeoutIfTimeoutHasValueAndSagaNotCompleted()
            {
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid(), Timeout = SystemTime.Now.AddMinutes(1) };
                var cachedItems = timeoutDispatcher.TimeoutCache.Count + 1;

                using (var context = new SagaContext(saga.GetType(), saga.CorrelationId, new FakeEvent()))
                {
                    context.TimeoutChanged = true;
                    timeoutDispatcher.PostSave(saga, context, null);
                }

                Assert.Equal(cachedItems, timeoutDispatcher.TimeoutCache.Count);
            }

            [Fact]
            public void UpdateTimerIfScheduledTimeoutBeforeNextCachedTimeout()
            {
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid(), Timeout = SystemTime.Now.AddMinutes(1) };

                using (var context = new SagaContext(saga.GetType(), saga.CorrelationId, new FakeEvent()))
                {
                    context.TimeoutChanged = true;
                    timeoutDispatcher.PostSave(saga, context, null);
                }

                Assert.True(timer.Changed);
            }

            [Fact]
            public void DoNotUpdateTimerIfScheduledTimeoutAfterNextCachedTimeout()
            {
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid(), Timeout = SystemTime.Now.AddMinutes(6) };

                using (var context = new SagaContext(saga.GetType(), saga.CorrelationId, new FakeEvent()))
                {
                    context.TimeoutChanged = true;
                    timeoutDispatcher.PostSave(saga, context, null);
                }

                Assert.False(timer.Changed);
            }
        }

        public class WhenDispatchingCommits : IDisposable
        {
            private readonly Mock<IStoreSagas> sagaStore;
            private readonly Mock<IPublishEvents> eventPublisher;
            private readonly TimeoutDispatcher timeoutDispatcher;
            private readonly SagaTimeout sagaTimeout;
            private readonly DateTime now;
            private FakeTimer timer;

            public WhenDispatchingCommits()
            {
                now = DateTime.UtcNow;
                sagaStore = new Mock<IStoreSagas>();
                eventPublisher = new Mock<IPublishEvents>();
                sagaTimeout = new SagaTimeout(typeof(FakeSaga), GuidStrategy.NewGuid(), now.AddMinutes(-5));

                SystemTime.OverrideWith(() => now);
               
                timeoutDispatcher = new TimeoutDispatcher(new Lazy<IStoreSagas>(() => sagaStore.Object), new Lazy<IPublishEvents>(() => eventPublisher.Object), callback => timer = new FakeTimer(callback));
                sagaStore.Setup(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>())).Returns(new[] { sagaTimeout });
            }

            public void Dispose()
            {
                SystemTime.ClearOverride();
            }

            [Fact]
            public void RescheduleTimerOnError()
            {
                eventPublisher.Setup(mock => mock.Publish(It.IsAny<IEnumerable<Header>>(), It.IsAny<EventEnvelope>())).Throws(new InvalidOperationException());

                timer.InvokeCallback();

                Assert.True(timer.Changed);
            }

            [Fact]
            public void RescheduleTimerAfterDispatch()
            {
                timer.InvokeCallback();

                Assert.True(timer.Changed);
            }

            public void DispatchElapsedSagatTimeouts()
            {
                timer.InvokeCallback();

                eventPublisher.Verify(mock => mock.Publish(It.IsAny<IEnumerable<Header>>(), It.IsAny<EventEnvelope>()), Times.Once());
            }
        }

        public class WhenEnsuringElapsedTimeoutsDispatched : IDisposable
        {
            private readonly Mock<IPublishEvents> eventPublisher;
            private readonly Mock<IStoreSagas> sagaStore;
            private readonly DateTime now;
            private FakeTimer timer;

            public WhenEnsuringElapsedTimeoutsDispatched()
            {
                now = DateTime.UtcNow;
                SystemTime.OverrideWith(() => now);
                sagaStore = new Mock<IStoreSagas>();
                eventPublisher = new Mock<IPublishEvents>();
            }

            public void Dispose()
            {
                SystemTime.ClearOverride();
            }

            [Fact]
            public void RescheduleTimer()
            {
                var dispatcher = new TimeoutDispatcher(new Lazy<IStoreSagas>(() => sagaStore.Object), new Lazy<IPublishEvents>(() => eventPublisher.Object), callback => timer = new FakeTimer(callback));

                sagaStore.Setup(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>())).Returns(new SagaTimeout[0]);
                eventPublisher.Setup(mock => mock.Publish(It.IsAny<IEnumerable<Header>>(), It.IsAny<EventEnvelope>())).Throws(new InvalidOperationException());
                dispatcher.EnsureElapsedTimeoutsDispatched();
                timer.InvokeCallback();

                Assert.True(timer.Changed);
                sagaStore.Verify(mock => mock.GetScheduledTimeouts(It.IsAny<DateTime>()), Times.Once());
            }
        }

        internal class FakeSaga : Saga
        {
            protected override void Configure(SagaConfiguration saga)
            { }
        }

        internal class FakeEvent : Event
        { }

        internal class FakeTimer : ITimer
        {
            private readonly Action callback;

            public Boolean Changed { get; private set; }

            public FakeTimer(Action callback)
            {
                Verify.NotNull(callback, "callback");

                this.callback = callback;
            }

            public void Reset()
            {
                Changed = false;
            }

            public void InvokeCallback()
            {
                callback();
            }

            public void Dispose()
            { }

            public void Change(TimeSpan dueTime, TimeSpan period)
            {
                Change(dueTime.Ticks / TimeSpan.TicksPerMillisecond, period.Ticks / TimeSpan.TicksPerMillisecond);
            }

            public void Change(Int64 dueTime, Int64 period)
            {
                Changed = true;
            }
        }
    }
}
