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

        public class WhenConvertingToArray
        {
            [Fact]
            public void WrapObjectInArray()
            {
                var value = new Object();
                var result = value.AsArray();

                Assert.Equal(1, result.Length);
                Assert.Same(value, result[0]);
            }
        }

        public class WhenConvertingToList
        {
            [Fact]
            public void WrapObjectInList()
            {
                var value = new Object();
                var result = value.AsList();

                Assert.Equal(1, result.Count);
                Assert.Same(value, result[0]);
            }
        }

        public class WhenConvertingToLowerString
        {
            [Fact]
            public void ReturnNullIfStringNull()
            {
                Assert.Null(default(Object).ToLowerInvariant());
            }

            [Fact]
            public void ReturnLowerCaseStringIfNotNull()
            {
                Object value = "MyPascalCaseString";
                Assert.Equal("mypascalcasestring", value.ToLowerInvariant());
            }
        }

        public class WhenConvertingToUpperString
        {
            [Fact]
            public void ReturnNullIfStringNull()
            {
                Assert.Null(default(Object).ToUpperInvariant());
            }

            [Fact]
            public void ReturnLowerCaseStringIfNotNull()
            {
                Object value = "MyPascalCaseString";
                Assert.Equal("MYPASCALCASESTRING", value.ToUpperInvariant());
            }
        }
    }
}
#pragma warning restore 1720
