using System;
using Spark.EventStore;
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

namespace Test.Spark.EventStore
{
    namespace UsingCommit
    {
        public class WhenCreatingNewCommit
        {
            [Fact]
            public void CorrelationIdCannotBeEmptyGuid()
            {
                var ex = Assert.Throws<ArgumentException>(() => new Commit(Guid.Empty, Guid.NewGuid(), 1, null, null));

                Assert.Equal("correlationId", ex.ParamName);
            }

            [Fact]
            public void StreamIdCannotBeEmptyGuid()
            {
                var ex = Assert.Throws<ArgumentException>(() => new Commit(Guid.NewGuid(), Guid.Empty, 1, null, null));

                Assert.Equal("streamId", ex.ParamName);
            }

            [Fact]
            public void RevisionGreaterThanZero()
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Commit(Guid.NewGuid(), Guid.NewGuid(), 0, null, null));

                Assert.Equal("version", ex.ParamName);
            }

            [Fact]
            public void HeadersCannotBeNull()
            {
                var commit = new Commit(Guid.NewGuid(), Guid.NewGuid(), 1, null, null);

                Assert.NotNull(commit.Headers);
            }

            [Fact]
            public void EventsCannotBeNull()
            {
                var commit = new Commit(Guid.NewGuid(), Guid.NewGuid(), 1, null, null);

                Assert.NotNull(commit.Events);
            }
        }
    }
}
