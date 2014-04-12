using System;
using JetBrains.Annotations;
using Spark;
using Spark.Cqrs.Eventing;
using Spark.Cqrs.Eventing.Sagas;
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
    namespace UsingSagaConfiguration
    {
        public class WhenConfiguringCanStartWithEvents
        {
            [Fact]
            public void ResolverCannotBeNull()
            {
                var configuration = new SagaConfiguration(typeof(FakeSaga));

                Assert.Throws<ArgumentNullException>(() => configuration.CanStartWith(default(Func<FakeEvent, Guid>)));
            }

            [Fact]
            public void CanRegisterEventTypeOnlyOnce()
            {
                var configuration = new SagaConfiguration(typeof(FakeSaga));

                configuration.CanStartWith((FakeEvent e) => e.Id);

                Assert.Equal(
                    Exceptions.EventTypeAlreadyConfigured.FormatWith(typeof(FakeSaga), typeof(FakeEvent)),
                    Assert.Throws<ArgumentException>(() => configuration.CanStartWith((FakeEvent e) => e.Id)).Message
                );
            }

            [Fact]
            public void EventMarkedAsInitiatingEvent()
            {
                var configuration = new SagaConfiguration(typeof(FakeSaga));

                configuration.CanStartWith((FakeEvent e) => e.Id);

                Assert.True(configuration.GetMetadata().CanStartWith(typeof(FakeEvent)));
            }
        }

        public class WhenConfiguringHandleEvents
        {
            [Fact]
            public void ResolverCannotBeNull()
            {
                var configuration = new SagaConfiguration(typeof(FakeSaga));

                Assert.Throws<ArgumentNullException>(() => configuration.CanHandle(default(Func<FakeEvent, Guid>)));
            }

            [Fact]
            public void CanRegisterEventTypeOnlyOnce()
            {
                var configuration = new SagaConfiguration(typeof(FakeSaga));

                configuration.CanStartWith((FakeEvent e) => e.Id);

                Assert.Equal(
                    Exceptions.EventTypeAlreadyConfigured.FormatWith(typeof(FakeSaga), typeof(FakeEvent)),
                    Assert.Throws<ArgumentException>(() => configuration.CanHandle((FakeEvent e) => e.Id)).Message
                );
            }

            [Fact]
            public void EventMarkedAsInitiatingEvent()
            {
                var configuration = new SagaConfiguration(typeof(FakeSaga));

                configuration.CanHandle((FakeEvent e) => e.Id);

                Assert.True(configuration.GetMetadata().CanHandle(typeof(FakeEvent)));
            }
        }

        internal sealed class FakeSaga : Saga
        {
            protected override void Configure(SagaConfiguration saga)
            {
                saga.CanStartWith(((FakeEvent e) => e.Id));
            }

            [UsedImplicitly]
            public void Handle(FakeEvent e)
            { }
        }

        internal sealed class FakeEvent : Event
        {
            [UsedImplicitly]
            public Guid Id { get; private set; }
        }
    }
}
