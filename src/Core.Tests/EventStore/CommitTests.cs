using System;
using System.Collections.Generic;
using System.IO;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Serialization;
using Xunit;

namespace Spark.Infrastructure.Tests.EventStore
{
    public static class UsingCommit
    {
        public class WhenCreatingNewCommit
        {
            [Fact]
            public void CommitIdCannotBeEmptyGuid()
            {
                var ex = Assert.Throws<ArgumentException>(() => new Commit(Guid.Empty, Guid.NewGuid(), 1, null, null));

                Assert.Equal("commitId", ex.ParamName);
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

                Assert.Equal("revision", ex.ParamName);
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

        public class WhenUsingBinarySerializer
        {
            [Fact]
            public void CanSerializeAndDeserializeCommit()
            {
                var originalCommit = new Commit(Guid.NewGuid(), Guid.NewGuid(), 1, new EventCollection(new[] { new Object() }), new HeaderCollection(new Dictionary<String, Object> { { "Name", null } }));
                var serializer = new BinarySerializer();
                var binaryData = default(Byte[]);

                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, originalCommit);
                    binaryData = stream.ToArray();
                }

                using (var stream = new MemoryStream(binaryData, writable: false))
                {
                    var deserializedCommit = (Commit)serializer.Deserialize(stream);

                    Assert.Equal(originalCommit.CommitId, deserializedCommit.CommitId);
                }
            }
        }
    }
}
