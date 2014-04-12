using System;
using System.Threading;
using System.Threading.Tasks;
using Spark.Messaging;
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

namespace Test.Spark.Messaging
{
    namespace UsingBlockingCollectionMessageBus
    {
        public class WhenSendingMessage
        {
            [Fact]
            public void MessageCannotBeNull()
            {
                var bus = new BlockingCollectionMessageBus<Object>();
                var ex = Assert.Throws<ArgumentNullException>(() => bus.Send(null));

                Assert.Equal("message", ex.ParamName);
            }

            [Fact]
            public void CannotSendIfDisposed()
            {
                var bus = new BlockingCollectionMessageBus<Object>();

                bus.Dispose();

                Assert.Throws<ObjectDisposedException>(() => bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object())));
            }

            [Fact]
            public void BlockOnSendBlockedIfReachedBoundedCapacity()
            {
                var bus = new BlockingCollectionMessageBus<Object>(1);
                var send1 = new ManualResetEvent(false);
                var send2 = new ManualResetEvent(false);
                var receive = new ManualResetEvent(false);
                var dispose = new ManualResetEvent(false);

                Assert.Equal(1, bus.BoundedCapacity);

                // ReSharper disable AccessToDisposedClosure
                Task.Factory.StartNew(() =>
                    {
                        receive.WaitOne();
                        bus.Receive();
                        bus.Receive();
                        dispose.Set();
                    });

                Task.Factory.StartNew(() =>
                    {
                        bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));
                        send1.Set();
                    });

                Task.Factory.StartNew(() =>
                    {
                        send1.WaitOne();
                        bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));
                        send2.Set();
                    });
                // ReSharper restore AccessToDisposedClosure

                Assert.False(send2.WaitOne(TimeSpan.FromMilliseconds(100)));

                receive.Set();
                dispose.WaitOne();
                bus.Dispose();
            }
        }

        public class WhenReceivingMessage
        {
            [Fact]
            public void AlwaysReturnNullAfterDispose()
            {
                var bus = new BlockingCollectionMessageBus<Object>();

                bus.Dispose();

                Assert.Null(bus.Receive());
            }

            [Fact]
            public void BlockUntilMesage()
            {
                var bus = new BlockingCollectionMessageBus<Object>(1);
                var send = new ManualResetEvent(false);
                var ready = new ManualResetEvent(false);
                var received = new ManualResetEvent(false);

                Assert.Equal(1, bus.BoundedCapacity);

                // ReSharper disable AccessToDisposedClosure
                Task.Factory.StartNew(() =>
                    {
                        send.WaitOne();
                        bus.Receive();
                        received.Set();
                    });

                Task.Factory.StartNew(() =>
                    {
                        ready.WaitOne();
                        bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));
                        send.Set();
                    });
                // ReSharper restore AccessToDisposedClosure

                Assert.False(received.WaitOne(TimeSpan.FromMilliseconds(100)));
                ready.Set();
                received.WaitOne();
                bus.Dispose();
            }
        }

        public class WhenDisposing
        {
            [Fact]
            public void CanCallDisposeMultipleTimes()
            {
                var bus = new BlockingCollectionMessageBus<Object>();

                bus.Dispose();

                Assert.DoesNotThrow(() => bus.Dispose());
            }

            [Fact]
            public void WaitForBusDrain()
            {
                var bus = new BlockingCollectionMessageBus<Object>();
                var receiveReady = new ManualResetEvent(false);

                bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));
                bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));
                bus.Send(new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, new Object()));

                Task.Factory.StartNew(() =>
                    {
                        var message = default(Message<Object>);
                        do
                        {
                            // ReSharper disable AccessToDisposedClosure
                            receiveReady.Set();
                            message = bus.Receive();
                            Thread.Sleep(10);
                            // ReSharper restore AccessToDisposedClosure
                        } while (message != null);
                    });

                receiveReady.WaitOne();
                bus.Dispose();

                Assert.Equal(0, bus.Count);
            }
        }
    }
}
