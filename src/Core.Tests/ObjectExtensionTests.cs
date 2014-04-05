using System;
using Spark;
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

#pragma warning disable 1720
namespace Test.Spark
{
    public static class UsingObjectExtensions
    {
        public class WhenCheckingIfNull
        {
            [Fact]
            public void ReturnTrueIfNull()
            {
                Assert.True(default(Object).IsNull());
            }

            [Fact]
            public void ReturnFalseIfNotNull()
            {
                Assert.False(new Object().IsNull());
            }
        }

        public class WhenCheckingIfNotNull
        {
            [Fact]
            public void ReturnFalseIfNull()
            {
                Assert.False(default(Object).IsNotNull());
            }

            [Fact]
            public void ReturnTrueIfNotNull()
            {
                Assert.True(new Object().IsNotNull());
            }
        }

        public class WhenConvertingToEnumerable
        {
            [Fact]
            public void WrapObjectInArray()
            {
                var value = new Object();
                var result = value.ToEnumerable();

                Assert.IsType(typeof(Object[]), result);
            }
        }
    }
}
#pragma warning restore 1720
