using System;
using Spark.Cqrs.Eventing.Sagas;
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
    public static class UsingPipelineHook
    {
        public class WhenPreGetOverriden
        {
            [Fact]
            public void ImplementsPreGetIsTrue()
            {
                var pipelineHook = new TestHook();

                Assert.True(pipelineHook.ImplementsPreGet);
            }

            [Fact]
            public void ImplementsPostGetIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPostGet);
            }

            [Fact]
            public void ImplementsPreSaveIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPreSave);
            }

            [Fact]
            public void ImplementsPostSaveIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPostSave);
            }

            [Fact]
            public void BasePreGetCanBeIgnored()
            {
                Assert.DoesNotThrow(() => new TestHook().PreGet(null, Guid.Empty));
            }

            private sealed class TestHook : PipelineHook
            {
                public override void PreGet(Type aggregateType, Guid id) { base.PreGet(null, Guid.Empty); }
            }
        }

        public class WhenPostGetOverriden
        {
            [Fact]
            public void ImplementsPreGetIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPreGet);
            }

            [Fact]
            public void ImplementsPostGetIsTrue()
            {
                var pipelineHook = new TestHook();

                Assert.True(pipelineHook.ImplementsPostGet);
            }

            [Fact]
            public void ImplementsPreSaveIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPreSave);
            }

            [Fact]
            public void ImplementsPostSaveIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPostSave);
            }

            [Fact]
            public void BasePostGetCanBeIgnored()
            {
                Assert.DoesNotThrow(() => new TestHook().PostGet(null));
            }

            private sealed class TestHook : PipelineHook
            {
                public override void PostGet(Saga saga) { base.PostGet(null); }
            }
        }

        public class WhenPreSaveOverriden
        {
            [Fact]
            public void ImplementsPreGetIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPreGet);
            }

            [Fact]
            public void ImplementsPostGetIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPostGet);
            }

            [Fact]
            public void ImplementsPreSaveIsTrue()
            {
                var pipelineHook = new TestHook();

                Assert.True(pipelineHook.ImplementsPreSave);
            }

            [Fact]
            public void ImplementsPostSaveIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPostSave);
            }

            [Fact]
            public void BasePreSaveCanBeIgnored()
            {
                Assert.DoesNotThrow(() => new TestHook().PreSave(null, null));
            }

            private sealed class TestHook : PipelineHook
            {
                public override void PreSave(Saga saga, SagaContext context) { base.PreSave(null, null); }
            }
        }

        public class WhenPostSaveOverriden
        {
            [Fact]
            public void ImplementsPreGetIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPreGet);
            }

            [Fact]
            public void ImplementsPostGetIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPostGet);
            }

            [Fact]
            public void ImplementsPreSaveIsFalse()
            {
                var pipelineHook = new TestHook();

                Assert.False(pipelineHook.ImplementsPreSave);
            }

            [Fact]
            public void ImplementsPostSaveIsTrue()
            {
                var pipelineHook = new TestHook();

                Assert.True(pipelineHook.ImplementsPostSave);
            }

            [Fact]
            public void BasePostSaveCanBeIgnored()
            {
                Assert.DoesNotThrow(() => new TestHook().PostSave(null, null, null));
            }

            private sealed class TestHook : PipelineHook
            {
                public override void PostSave(Saga saga, SagaContext context, Exception error) { base.PostSave(null, null, null); }
            }
        }
    }
}
