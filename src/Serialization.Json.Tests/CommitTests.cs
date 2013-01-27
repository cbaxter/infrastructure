using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Spark.Infrastructure;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Serialization.Json;
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

namespace Serialization.Json.Tests
{
    public static class UsingCommit
    {
        public class WhenUsingJsonSerializer
        {
            [Fact]
            public void CanSerializeToJson()
            {
                var now = DateTime.UtcNow;
                var commitId = Guid.NewGuid();
                var streamId = Guid.NewGuid();
                var serializer = new JsonSerializer();

                SystemTime.OverrideWith(() => now);

                var commit = new Commit(commitId, streamId, 1, new EventCollection(new[] { new Object() }), new HeaderCollection(new Dictionary<String, Object> { { "Name", new Object() } }));

                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, commit);

                    Assert.Equal(
                        String.Format("﻿{{\"$type\":\"Spark.Infrastructure.EventStore.Commit, Spark.Infrastructure.Core\",\"c\":\"{0}\",\"s\":\"{1}\",\"r\":1,\"t\":\"{2:yyyy-MM-ddTHH:mm:ss.fffffffZ}\",\"e\":[{{\"$type\":\"System.Object, mscorlib\"}}],\"h\":{{\"Name\":{{\"$type\":\"System.Object, mscorlib\"}}}}}}", commitId, streamId, now),
                        Encoding.UTF8.GetString(stream.ToArray()).Trim()
                    );
                }
            }

            [Fact]
            public void CanDeserializeFromJson()
            {
                var json = "﻿{\"$type\":\"Spark.Infrastructure.EventStore.Commit, Spark.Infrastructure.Core\",\"c\":\"03d53ae2-468a-48c8-9dc7-45aa43edf982\",\"s\":\"f11cee78-049a-454f-b9bc-2031f7171ab9\",\"r\":1,\"e\":[{\"$type\":\"System.Object, mscorlib\"}],\"h\":{\"Name\":{\"$type\":\"System.Object, mscorlib\"}}}";
                var serializer = new JsonSerializer();

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json), writable: false))
                {
                    var commit = (Commit)serializer.Deserialize(stream);

                    Assert.Equal(Guid.Parse("03d53ae2-468a-48c8-9dc7-45aa43edf982"), commit.CommitId);
                }
            }
        }
    }
}
