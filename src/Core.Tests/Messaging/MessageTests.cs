using System;
using Spark.Infrastructure.Messaging;
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

namespace Spark.Infrastructure.Tests.Messaging
{
    public static class UsingMessage
    {
        public class WhenCreatingMessage
        {
            [Fact]
            public void IdentifierCannotBeEmptyGuid()
            {
                var ex = Assert.Throws<ArgumentException>(() => new Message<Object>(Guid.Empty, HeaderCollection.Empty, new Object()));

                Assert.Equal("id", ex.ParamName);
            }

            [Fact]
            public void HeadersCollectionCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new Message<Object>(Guid.NewGuid(), null, new Object()));

                Assert.Equal("headers", ex.ParamName);
            }

            [Fact]
            public void PayloadCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, null));

                Assert.Equal("payload", ex.ParamName);
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnFriendlyDescription()
            {
                var id = Guid.NewGuid();
                var message = new Message<Object>(id, HeaderCollection.Empty, new Object());

                Assert.Equal(String.Format("{0} - System.Object", id), message.ToString());
            }
        }
    }
}
