using System;
using Spark.Cqrs.Eventing.Sagas;
using Xunit;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Test.Spark.Cqrs.Eventing.Sagas
{
    namespace UsingPipelineHook
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
                var hook = new TestHook();

                hook.PreGet(null, Guid.Empty);

                Assert.True(hook.PreGetInvoked);
            }

            private sealed class TestHook : PipelineHook
            {
                public Boolean PreGetInvoked { get; private set; }
                public override void PreGet(Type aggregateType, Guid id) { base.PreGet(null, Guid.Empty); PreGetInvoked = true; }
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
                var hook = new TestHook();

                hook.PostGet(null);

                Assert.True(hook.PostGetInvoked);
            }

            private sealed class TestHook : PipelineHook
            {
                public Boolean PostGetInvoked { get; private set; }
                public override void PostGet(Saga saga) { base.PostGet(null); PostGetInvoked = true; }
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
                var hook = new TestHook();

                hook.PreSave(null, null);

                Assert.True(hook.PreSaveInvoked);
            }

            private sealed class TestHook : PipelineHook
            {
                public Boolean PreSaveInvoked { get; private set; }
                public override void PreSave(Saga saga, SagaContext context) { base.PreSave(null, null); PreSaveInvoked = true; }
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
                var hook = new TestHook();

                hook.PostSave(null, null, null);

                Assert.True(hook.PostSaveInvoked);
            }

            private sealed class TestHook : PipelineHook
            {
                public Boolean PostSaveInvoked { get; private set; }
                public override void PostSave(Saga saga, SagaContext context, Exception error) { base.PostSave(null, null, null); PostSaveInvoked = true; }
            }
        }
    }
}
