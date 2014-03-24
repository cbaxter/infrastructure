using System;
using Spark.Cqrs.Eventing;
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

namespace Test.Spark.Cqrs.Eventing
{
    public static class UsingEventVersion
    {
        public class WhenCreatingNewVersion
        {
            [Fact]
            public void VersionMustBeGreaterThanZero()
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new EventVersion(0, 1, 1));

                Assert.Equal("version", ex.ParamName);
            }

            [Fact]
            public void CountMustBeGreaterThanOrEqualToZero()
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new EventVersion(1, -1, 1));

                Assert.Equal("count", ex.ParamName);
            }

            [Fact]
            public void ItemMustBeGreaterThanOrEqualToZero()
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new EventVersion(1, 1, -1));

                Assert.Equal("item", ex.ParamName);
            }

            [Fact]
            public void ItemMustBeLessThanOrEqualToCount()
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new EventVersion(1, 3, 4));

                Assert.Equal("item", ex.ParamName);
            }
        }

        public class WhenComparingVersions
        {
            [Fact]
            public void LowerVersionBeforeHigherVersion()
            {
                var version1 = new EventVersion(1, 2, 2);
                var version2 = new EventVersion(2, 1, 1);

                Assert.Equal(-1, version1.CompareTo(version2));
            }

            [Fact]
            public void HigherVersionAfterLowerVersion()
            {
                var version1 = new EventVersion(1, 2, 2);
                var version2 = new EventVersion(2, 1, 1);

                Assert.Equal(1, version2.CompareTo(version1));
            }

            [Fact]
            public void LowerItemBeforeHigherItemInSameVersion()
            {
                var version1 = new EventVersion(1, 2, 1);
                var version2 = new EventVersion(1, 2, 2);

                Assert.Equal(-1, version1.CompareTo(version2));
            }

            [Fact]
            public void HigherItemAfterLowerItemInSameVersion()
            {
                var version1 = new EventVersion(1, 2, 1);
                var version2 = new EventVersion(1, 2, 2);

                Assert.Equal(1, version2.CompareTo(version1));
            }
        }

        public class WhenTestingEquality
        {
            [Fact]
            public void VersionMustBeEqual()
            {
                Assert.False(new EventVersion(1, 1, 1).Equals((Object)new EventVersion(2, 1, 1)));
            }

            [Fact]
            public void CountMustBeEqual()
            {
                Assert.False(new EventVersion(1, 1, 1).Equals((Object)new EventVersion(1, 2, 1)));
            }

            [Fact]
            public void ItemMustBeEqual()
            {
                Assert.False(new EventVersion(1, 2, 1).Equals((Object)new EventVersion(1, 2, 2)));
            }

            public void ReturnTrueIfAllValuesEqual()
            {
                Assert.True(new EventVersion(1, 1, 1).Equals((Object)new EventVersion(1, 1, 1)));
            }
        }

        public class WhenGettingHashCode
        {
            [Fact]
            public void AlwaysReturnConsistentValue()
            {
                var version1 = new EventVersion(1, 1, 1);
                var version2 = new EventVersion(1, 1, 1);

                Assert.Equal(version1.GetHashCode(), version2.GetHashCode());
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnFriendlyDescription()
            {
                var version = new EventVersion(1, 1, 1);

                Assert.Equal("1 (Event 1 of 1)", version.ToString());
            }
        }
    }
}
