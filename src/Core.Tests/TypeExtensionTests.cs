using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Test.Spark
{
    public static class UsingTypeExtensions
    {
        public class WhenCheckingTypeDerivesFromAnotherType
        {
            [Fact]
            public void TypeCanBeGenericTypeDefinition()
            {
                Assert.True(typeof(CustomList).DerivesFrom(typeof(List<>)));
            }

            [Fact]
            public void ReturnTrueIfTypeFound()
            {
                Assert.True(typeof(CustomList).DerivesFrom(typeof(Object)));
            }

            [Fact]
            public void ReturnFalseIfTypeNotFound()
            {
                Assert.False(typeof(CustomList).DerivesFrom(typeof(Dictionary<,>)));
            }

            private class CustomList : List<Object>
            { }
        }

        public class WhenFindingBaseType
        {
            [Fact]
            public void ReturnTypeWithGenericTypeParameters()
            {
                Assert.Equal(typeof(List<Object>), typeof(CustomList).FindBaseType(typeof(List<>)));
            }

            [Fact]
            public void ReturnNullIfNotFound()
            {
                Assert.Null(typeof(CustomList).FindBaseType(typeof(Dictionary<,>)));
            }

            private class CustomList : List<Object>
            { }
        }

        public class WhenRetrievingTypeHierarchy
        {
            [Fact]
            public void FirstEntryAlwaysCurrentType()
            {
                Assert.Equal(typeof(CustomList), typeof(CustomList).GetTypeHierarchy().First());
            }

            [Fact]
            public void IntermediateEntriesAreBaseTypes()
            {
                Assert.Equal(typeof(List<Object>), typeof(CustomList).GetTypeHierarchy().Skip(1).First());
            }

            [Fact]
            public void LastEntryAlwaysObjectType()
            {
                Assert.Equal(typeof(Object), typeof(CustomList).GetTypeHierarchy().Last());
            }

            private class CustomList : List<Object>
            { }
        }
    }
}
