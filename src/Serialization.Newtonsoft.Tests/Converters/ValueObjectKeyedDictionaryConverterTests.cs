using System;
using System.Collections.Generic;
using Spark;
using Xunit;

/* Copyright (c) 2014 Spark Software Ltd.
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
    namespace UsingValueObjectKeyedDictionaryConverter
    {
        internal sealed class TestObject : ValueObject<Guid>
        {
            public TestObject()
                : this(Guid.NewGuid())
            { }

            public TestObject(Guid value)
                : base(value)
            { }

            protected override Boolean TryParse(String value, out Guid result)
            {
                return Guid.TryParse(value, out result);
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Null(ReadJson<Dictionary<TestObject, Int32>>("null"));
            }

            [Fact]
            public void CanDeserializeDictionaryToJson()
            {
                var json = "{\"b228c143-521b-8253-37fc-b1d344180000\":1,\"ea28c143-e31b-8253-37fc-b1d344180000\":2}";
                var value = ReadJson<Dictionary<TestObject, Int32>>(json);

                Assert.True(value.ContainsKey(new TestObject(Guid.Parse("b228c143-521b-8253-37fc-b1d344180000"))));
                Assert.True(value.ContainsKey(new TestObject(Guid.Parse("ea28c143-e31b-8253-37fc-b1d344180000"))));
            }
        }
    }
}
