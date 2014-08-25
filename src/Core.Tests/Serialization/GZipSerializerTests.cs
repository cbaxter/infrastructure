using System;
using System.IO;
using Spark.Serialization;
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

namespace Test.Spark.Serialization
{
    namespace UsingGZipSerializer
    {
        public sealed class WhenSerializingStream
        {
            [Fact]
            public void CompressBaseSerializerOutput()
            {
                var baseSerializer = new BinarySerializer();
                var gzipSerializer = new GZipSerializer(baseSerializer);

                using (var memoryStream = new MemoryStream())
                {
                    gzipSerializer.Serialize(memoryStream, "My Object", typeof(String));
                    Assert.Equal("H4sIAAAAAAAEAGNgZGBg+A8EIBoE2EAMTt9KBf+krNTkEm4ASBYMlCEAAAA=", Convert.ToBase64String(memoryStream.ToArray()));
                }
            }
        }

        public sealed class WhenDeserializingStream
        {
            [Fact]
            public void DecompressBaseSerializerOutput()
            {
                var baseSerializer = new BinarySerializer();
                var gzipSerializer = new GZipSerializer(baseSerializer);

                using (var memoryStream = new MemoryStream(Convert.FromBase64String("H4sIAAAAAAAEAGNgZGBg+A8EIBoE2EAMTt9KBf+krNTkEm4ASBYMlCEAAAA=")))
                {
                    Assert.Equal("My Object", gzipSerializer.Deserialize(memoryStream, typeof(String)));
                }
            }
        }
    }
}
