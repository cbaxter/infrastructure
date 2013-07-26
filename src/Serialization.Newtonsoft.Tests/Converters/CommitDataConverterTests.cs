using System;
using Spark.Cqrs.Eventing;
using Spark.EventStore;
using Spark.Messaging;
using Spark.Serialization.Converters;
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

namespace Test.Spark.Serialization.Converters
{
    public static class UsingCommitDataConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(new CommitDataConverter(), default(CommitData));

                Validate("{\"h\":{},\"e\":[]}", json);
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var data = new CommitData(HeaderCollection.Empty, EventCollection.Empty);
                var json = WriteJson(new CommitDataConverter(), data);

                Validate("{\"h\":{},\"e\":[]}", json);
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Equal(CommitData.Empty, ReadJson<CommitData>(new CommitDataConverter(), "null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var json = "﻿{\"h\":{},\"e\":[]}";
                var data = ReadJson<CommitData>(new CommitDataConverter(), json);

                Assert.Equal(HeaderCollection.Empty, data.Headers);
            }

            [Fact]
            public void PropertyOrderIrrelevant()
            {
                var json = "{\"e\":[],\"h\":{}}";
                var data = ReadJson<CommitData>(new CommitDataConverter(), json);

                Assert.Equal(HeaderCollection.Empty, data.Headers);
            }

            [Fact]
            public void CanTolerateMalformedJson()
            {
                var json = "{\"e\":[],\"h\":{},}";
                var data = ReadJson<CommitData>(new CommitDataConverter(), json);

                Assert.Equal(HeaderCollection.Empty, data.Headers);
            }
        }

        public class WhenWritingBson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeToBson()
            {
                var data = new CommitData(HeaderCollection.Empty, EventCollection.Empty);
                var json = WriteBson(new CommitDataConverter(), data);

                Validate("﻿FQAAAANoAAUAAAAABGUABQAAAAAA", json);
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var bson = "FQAAAANoAAUAAAAABGUABQAAAAAA";
                var data = ReadBson<CommitData>(new CommitDataConverter(), bson);

                Assert.Equal(HeaderCollection.Empty, data.Headers);
            }
        }
    }
}
