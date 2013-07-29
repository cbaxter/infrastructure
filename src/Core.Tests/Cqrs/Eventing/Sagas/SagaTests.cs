using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Spark;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Mappings;
using Spark.Cqrs.Eventing.Sagas;
using Spark.Messaging;
using Spark.Resources;
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
    public static class UsingSaga
    {
        public class WhenGettingSagaMetadata
        {
            [Fact]
            public void AllHandledEventsMustBeConfigured()
            {
                var knownHandleMethods = new HandleMethodCollection(new Dictionary<Type, Action<Object, Event>> { { typeof(FakeEvent), (handler, e) => { } } });
                var ex = Assert.Throws<MappingException>(() => Saga.GetMetadata(typeof(SagaWithEventTypesNotConfigured), knownHandleMethods));

                Assert.Equal(Exceptions.EventTypeNotConfigured.FormatWith(typeof(SagaWithEventTypesNotConfigured), typeof(FakeEvent)), ex.Message);
            }

            [Fact]
            public void MustHaveAtLeastOneInitiatingEvent()
            {
                var knownHandleMethods = new HandleMethodCollection(new Dictionary<Type, Action<Object, Event>> { { typeof(FakeEvent), (handler, e) => { } } });
                var ex = Assert.Throws<MappingException>(() => Saga.GetMetadata(typeof(SagaWithInitiatingEventNotConfigured), knownHandleMethods));

                Assert.Equal(Exceptions.SagaMustHaveAtLeastOneInitiatingEvent.FormatWith(typeof(SagaWithInitiatingEventNotConfigured)), ex.Message);
            }

            private class SagaWithEventTypesNotConfigured : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                { }
            }

            private class SagaWithInitiatingEventNotConfigured : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanHandle((FakeEvent e) => e.Id);
                }
            }
        }

        public class WhenLockingSaga
        {
            [Fact]
            public void CanExplicitylyReleaseLock()
            {
                Saga.ReleaseLock(Saga.AquireLock(typeof(Saga), GuidStrategy.NewGuid()));
            }
        }

        public class WhenCompletingSagas
        {
            [Fact]
            public void FlagSagaAsCompleteOnMarkCompleted()
            {
                var saga = new FakeSaga();

                saga.Handle(new FakeEvent());

                Assert.True(saga.Completed);
            }

            private class FakeSaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanStartWith((FakeEvent e) => e.Id);
                }

                public void Handle(FakeEvent e)
                {
                    MarkCompleted();
                }
            }
        }

        public class WhenSchedulingTimeout
        {
            [Fact]
            public void CanScheduleTimeoutIfNotScheduled()
            {
                var saga = new FakeSaga();

                saga.Handle(new FakeEvent());

                Assert.NotNull(saga.Timeout);
            }

            [Fact]
            public void TimeoutRepresentedAsUtcDateTime()
            {
                var saga = new FakeSaga();

                saga.Handle(new FakeEvent());

                Assert.Equal(DateTimeKind.Utc, saga.Timeout.GetValueOrDefault().Kind);
            }

            [Fact]
            public void CannotScheduleTimeoutIfAlreadyScheduled()
            {
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid(), Timeout = SystemTime.Now };
                var ex = Assert.Throws<InvalidOperationException>(() => saga.Handle(new FakeEvent()));

                Assert.Equal(Exceptions.SagaTimeoutAlreadyScheduled.FormatWith(saga.GetType(), saga.CorrelationId), ex.Message);
            }

            private class FakeSaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanStartWith((FakeEvent e) => e.Id);
                }

                public void Handle(FakeEvent e)
                {
                    ScheduleTimeout(TimeSpan.FromMinutes(30));
                }
            }
        }

        public class WhenSchedulingExplicitTimeout
        {
            [Fact]
            public void WillConvertDateTimeToUtcIfUnknown()
            {
                var saga = new FakeSaga();

                saga.Handle(new FakeEvent());

                Assert.Equal(DateTimeKind.Utc, saga.Timeout.GetValueOrDefault().Kind);
            }

            private class FakeSaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanStartWith((FakeEvent e) => e.Id);
                }

                public void Handle(FakeEvent e)
                {
                    ScheduleTimeout(DateTime.Now);
                }
            }
        }

        public class WhenClearingTimeout
        {
            [Fact]
            public void CanClearTimeoutIfScheduled()
            {
                var saga = new FakeSaga { Timeout = SystemTime.Now };

                saga.Handle(new FakeEvent());

                Assert.Null(saga.Timeout);
            }

            [Fact]
            public void CannotClearTimeoutIfNotScheduled()
            {
                var saga = new FakeSaga { CorrelationId = GuidStrategy.NewGuid(), Timeout = null };
                var ex = Assert.Throws<InvalidOperationException>(() => saga.Handle(new FakeEvent()));

                Assert.Equal(Exceptions.SagaTimeoutNotScheduled.FormatWith(saga.GetType(), saga.CorrelationId), ex.Message);
            }

            private class FakeSaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanStartWith((FakeEvent e) => e.Id);
                }

                public void Handle(FakeEvent e)
                {
                    ClearTimeout();
                }
            }
        }

        public class WhenReschedulingTimeout
        {
            [Fact]
            public void CanRescheduleIfTimeoutAlreadyScheduled()
            {
                var timeout = SystemTime.Now;
                var saga = new FakeSaga { Timeout = timeout };

                saga.Handle(new FakeEvent());

                Assert.NotEqual(timeout, saga.Timeout);
            }

            [Fact]
            public void CanRescheduleIfTimeoutNotAlreadyScheduled()
            {
                var saga = new FakeSaga { Timeout = null };

                saga.Handle(new FakeEvent());

                Assert.NotNull(saga.Timeout);
            }

            private class FakeSaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanStartWith((FakeEvent e) => e.Id);
                }

                public void Handle(FakeEvent e)
                {
                    RescheduleTimeout(TimeSpan.FromMinutes(30));
                }
            }
        }

        public class WhenPublishingCommandWithNoCustomHeaders
        {
            [Fact]
            public void SagaContextIsRequired()
            {
                var saga = new FakeSaga();
                var ex = Assert.Throws<InvalidOperationException>(() => saga.Handle(new FakeEvent()));

                Assert.Equal(Exceptions.NoSagaContext, ex.Message);
            }

            [Fact]
            public void EventContextIsRequired()
            {
                var saga = new FakeSaga();

                using (new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), new FakeEvent()))
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => saga.Handle(new FakeEvent()));

                    Assert.Equal(Exceptions.NoEventContext, ex.Message);
                }
            }

            [Fact]
            public void CopyUserAddressHeaderFromEventContext()
            {
                var saga = new FakeSaga();
                var userAddress = IPAddress.Loopback.ToString();
                var e = new FakeEvent { Id = GuidStrategy.NewGuid() };
                var headers = new HeaderCollection(new Dictionary<String, string> { { Header.UserAddress, userAddress } });

                using (new EventContext(e.Id, headers, e))
                using (var sagaContext = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), e))
                {
                    saga.Handle(new FakeEvent());

                    Assert.Equal(userAddress, sagaContext.GetPublishedCommands().Single().Headers.Single().Value);
                }
            }

            [Fact]
            public void CopyRemoteAddressHeaderFromEventContextIfNoUserAddress()
            {
                var saga = new FakeSaga();
                var userAddress = IPAddress.Loopback.ToString();
                var e = new FakeEvent { Id = GuidStrategy.NewGuid() };
                var headers = new HeaderCollection(new Dictionary<String, string> { { Header.RemoteAddress, userAddress } });

                using (new EventContext(e.Id, headers, e))
                using (var sagaContext = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), e))
                {
                    saga.Handle(new FakeEvent());

                    Assert.Equal(userAddress, sagaContext.GetPublishedCommands().Single().Headers.Single().Value);
                }
            }

            [Fact]
            public void CopyUserNameHeaderFromEventContext()
            {
                var saga = new FakeSaga();
                var userName = Guid.NewGuid().ToString();
                var e = new FakeEvent { Id = GuidStrategy.NewGuid() };
                var headers = new HeaderCollection(new Dictionary<String, string> { { Header.UserName, userName } });

                using (new EventContext(e.Id, headers, e))
                using (var sagaContext = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), e))
                {
                    saga.Handle(new FakeEvent());

                    Assert.Equal(userName, sagaContext.GetPublishedCommands().Single().Headers.Single().Value);
                }
            }

            private class FakeSaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanStartWith((FakeEvent e) => e.Id);
                }

                public void Handle(FakeEvent e)
                {
                    Publish(e.Id, new FakeCommand());
                }
            }
        }

        public class WhenPublishingCommandWithParamHeaders
        {
            [Fact]
            public void SagaContextIsRequired()
            {
                var saga = new FakeSaga();
                var ex = Assert.Throws<InvalidOperationException>(() => saga.Handle(new FakeEvent()));

                Assert.Equal(Exceptions.NoSagaContext, ex.Message);
            }

            [Fact]
            public void EventContextIsRequired()
            {
                var saga = new FakeSaga();

                using (new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), new FakeEvent()))
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => saga.Handle(new FakeEvent()));

                    Assert.Equal(Exceptions.NoEventContext, ex.Message);
                }
            }

            [Fact]
            public void CopyUserAddressHeaderFromEventContext()
            {
                var saga = new FakeSaga();
                var userAddress = IPAddress.Loopback.ToString();
                var e = new FakeEvent { Id = GuidStrategy.NewGuid() };
                var headers = new HeaderCollection(new Dictionary<String, string> { { Header.UserAddress, userAddress } });

                using (new EventContext(e.Id, headers, e))
                using (var sagaContext = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), e))
                {
                    saga.Handle(new FakeEvent());

                    Assert.Equal(userAddress, sagaContext.GetPublishedCommands().Single().Headers.Single().Value);
                }
            }

            [Fact]
            public void CopyRemoteAddressHeaderFromEventContextIfNoUserAddress()
            {
                var saga = new FakeSaga();
                var userAddress = IPAddress.Loopback.ToString();
                var e = new FakeEvent { Id = GuidStrategy.NewGuid() };
                var headers = new HeaderCollection(new Dictionary<String, string> { { Header.RemoteAddress, userAddress } });

                using (new EventContext(e.Id, headers, e))
                using (var sagaContext = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), e))
                {
                    saga.Handle(new FakeEvent());

                    Assert.Equal(userAddress, sagaContext.GetPublishedCommands().Single().Headers.Single().Value);
                }
            }

            [Fact]
            public void CopyUserNameHeaderFromEventContext()
            {
                var saga = new FakeSaga();
                var userName = Guid.NewGuid().ToString();
                var e = new FakeEvent { Id = GuidStrategy.NewGuid() };
                var headers = new HeaderCollection(new Dictionary<String, string> { { Header.UserName, userName } });

                using (new EventContext(e.Id, headers, e))
                using (var sagaContext = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), e))
                {
                    saga.Handle(new FakeEvent());

                    Assert.Equal(userName, sagaContext.GetPublishedCommands().Single().Headers.Single().Value);
                }
            }

            [Fact]
            public void PreferCustomHeaderOverEventContextHeaderIfDefined()
            {
                var saga = new FakeSaga();
                var userName = Guid.NewGuid().ToString();
                var customUserName = Guid.NewGuid().ToString();
                var e = new FakeEvent { Id = GuidStrategy.NewGuid() };
                var headers = new HeaderCollection(new Dictionary<String, string> { { Header.UserName, userName } });

                using (new EventContext(e.Id, headers, e))
                using (var sagaContext = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), e))
                {
                    saga.Handle(new FakeEvent { CustomHeaders = new[] { new Header(Header.UserName, customUserName, checkReservedNames: false) } });

                    Assert.Equal(customUserName, sagaContext.GetPublishedCommands().Single().Headers.Single().Value);
                }
            }

            private class FakeSaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanStartWith((FakeEvent e) => e.Id);
                }

                public void Handle(FakeEvent e)
                {
                    Publish(e.Id, new FakeCommand(), e.CustomHeaders);
                }
            }

            private class FakeEvent : Event
            {
                public Guid Id { get; set; }
                public Header[] CustomHeaders { get; set; }
            }
        }

        public class WhenPublishingCommandWithEnumerableHeaders
        {
            [Fact]
            public void SagaContextIsRequired()
            {
                var saga = new FakeSaga();
                var ex = Assert.Throws<InvalidOperationException>(() => saga.Handle(new FakeEvent()));

                Assert.Equal(Exceptions.NoSagaContext, ex.Message);
            }

            [Fact]
            public void EventContextIsRequired()
            {
                var saga = new FakeSaga();

                using (new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), new FakeEvent()))
                {
                    var ex = Assert.Throws<InvalidOperationException>(() => saga.Handle(new FakeEvent()));

                    Assert.Equal(Exceptions.NoEventContext, ex.Message);
                }
            }

            [Fact]
            public void CopyUserAddressHeaderFromEventContext()
            {
                var saga = new FakeSaga();
                var userAddress = IPAddress.Loopback.ToString();
                var e = new FakeEvent { Id = GuidStrategy.NewGuid() };
                var headers = new HeaderCollection(new Dictionary<String, string> { { Header.UserAddress, userAddress } });

                using (new EventContext(e.Id, headers, e))
                using (var sagaContext = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), e))
                {
                    saga.Handle(new FakeEvent());

                    Assert.Equal(userAddress, sagaContext.GetPublishedCommands().Single().Headers.Single().Value);
                }
            }

            [Fact]
            public void CopyRemoteAddressHeaderFromEventContextIfNoUserAddress()
            {
                var saga = new FakeSaga();
                var userAddress = IPAddress.Loopback.ToString();
                var e = new FakeEvent { Id = GuidStrategy.NewGuid() };
                var headers = new HeaderCollection(new Dictionary<String, string> { { Header.RemoteAddress, userAddress } });

                using (new EventContext(e.Id, headers, e))
                using (var sagaContext = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), e))
                {
                    saga.Handle(new FakeEvent());

                    Assert.Equal(userAddress, sagaContext.GetPublishedCommands().Single().Headers.Single().Value);
                }
            }

            [Fact]
            public void CopyUserNameHeaderFromEventContext()
            {
                var saga = new FakeSaga();
                var userName = Guid.NewGuid().ToString();
                var e = new FakeEvent { Id = GuidStrategy.NewGuid() };
                var headers = new HeaderCollection(new Dictionary<String, string> { { Header.UserName, userName } });

                using (new EventContext(e.Id, headers, e))
                using (var sagaContext = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), e))
                {
                    saga.Handle(new FakeEvent());

                    Assert.Equal(userName, sagaContext.GetPublishedCommands().Single().Headers.Single().Value);
                }
            }

            [Fact]
            public void PreferCustomHeaderOverEventContextHeaderIfDefined()
            {
                var saga = new FakeSaga();
                var userName = Guid.NewGuid().ToString();
                var customUserName = Guid.NewGuid().ToString();
                var e = new FakeEvent { Id = GuidStrategy.NewGuid() };
                var headers = new HeaderCollection(new Dictionary<String, string> { { Header.UserName, userName } });

                using (new EventContext(e.Id, headers, e))
                using (var sagaContext = new SagaContext(typeof(Saga), GuidStrategy.NewGuid(), e))
                {
                    saga.Handle(new FakeEvent { CustomHeaders = new[] { new Header(Header.UserName, customUserName, checkReservedNames: false) } });

                    Assert.Equal(customUserName, sagaContext.GetPublishedCommands().Single().Headers.Single().Value);
                }
            }

            private class FakeSaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanStartWith((FakeEvent e) => e.Id);
                }

                public void Handle(FakeEvent e)
                {
                    Publish(e.Id, new FakeCommand(), e.CustomHeaders);
                }
            }

            private class FakeEvent : Event
            {
                public Guid Id { get; set; }
                public IEnumerable<Header> CustomHeaders { get; set; }
            }
        }
        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnFriendlyDescription()
            {
                var correlationId = GuidStrategy.NewGuid();

                Assert.Equal(String.Format("{0} - {1}", typeof(FakeSaga), correlationId), new FakeSaga { CorrelationId = correlationId }.ToString());
            }

            private class FakeSaga : Saga
            {
                protected override void Configure(SagaConfiguration saga)
                {
                    saga.CanStartWith((FakeEvent e) => e.Id);
                }
            }
        }

        private class FakeEvent : Event
        {
            public Guid Id { get; set; }
        }

        private class FakeCommand : Command
        {
            public Guid Id { get; set; }
        }
    }
}
