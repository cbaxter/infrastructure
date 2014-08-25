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

#pragma warning disable 1720
namespace Test.Spark
{
    namespace UsingEnumerableExtensions
    {
        public class WhenConvertingToArray
        {
            [Fact]
            public void DoNothingIfAlreadyArray()
            {
                var expected = (IEnumerable<Object>)new Object[0];
                var actual = expected.AsArray();

                Assert.Same(expected, actual);
            }

            [Fact]
            public void ReturnEmptyArrayIfNull()
            {
                var actual = default(IEnumerable<Object>).AsArray();

                Assert.NotNull(actual);
                Assert.Equal(0, actual.Length);
            }

            [Fact]
            public void ReturnNewArrayIfNotAlreadyArray()
            {
                var actual = Enumerable.Empty<Object>().AsArray();

                Assert.NotNull(actual);
                Assert.Equal(0, actual.Length);
            }
        }

        public class WhenConvertingToList
        {
            [Fact]
            public void DoNothingIfAlreadyList()
            {
                var expected = (IEnumerable<Object>)new Object[0];
                var actual = expected.AsList();

                Assert.Same(expected, actual);
            }

            [Fact]
            public void ReturnEmptyListIfNull()
            {
                var actual = default(IEnumerable<Object>).AsList();

                Assert.NotNull(actual);
                Assert.Equal(0, actual.Count);
            }

            [Fact]
            public void ReturnNewListIfNotAlreadyList()
            {
                var actual = Enumerable.Empty<Object>().AsList();

                Assert.NotNull(actual);
                Assert.Equal(0, actual.Count);
            }
        }

        public class WhenConvertingToReadOnlyList
        {
            [Fact]
            public void DoNothingIfAlreadyReadOnlyList()
            {
                var expected = (IEnumerable<Object>)new Object[0];
                var actual = expected.AsReadOnly();

                Assert.Same(expected, actual);
            }

            [Fact]
            public void ReturnEmptyReadOnlyListIfNull()
            {
                var actual = default(IEnumerable<Object>).AsReadOnly();

                Assert.NotNull(actual);
                Assert.Equal(0, actual.Count);
            }

            [Fact]
            public void ReturnNewReadOnlyListIfNotAlreadyReadOnlyList()
            {
                var actual = Enumerable.Empty<Object>().AsReadOnly();

                Assert.NotNull(actual);
                Assert.Equal(0, actual.Count);
            }
        }

        public class WhenAppendingItemToSet
        {
            [Fact]
            public void CanAppendItemToNullSet()
            {
                var item = new Object();

                Assert.Same(item, ((IEnumerable<Object>)null).Concat(item).Single());
            }

            [Fact]
            public void CanAppendNullItemToExistingSet()
            {
                Assert.Null(Enumerable.Empty<Object>().Concat(default(Object)).Single());
            }

            [Fact]
            public void ItemAlwaysAddedToEndOfSet()
            {
                var item = new Object();
                var items = new[] { new Object(), new Object(), new Object() };

                Assert.Same(item, items.Concat(item).Last());
            }
        }

        public class WhenDisposingEnumerableSet
        {
            [Fact]
            public void SetCanBeNull()
            {
                Assert.DoesNotThrow(() => default(IDisposable[]).DisposeAll());
            }

            [Fact]
            public void DisposeEachItemInSet()
            {
                var disposable = new Disposable();
                var disposables = new[] {disposable};
                
                disposables.DisposeAll();

                Assert.True(disposable.Disposed);
            }

            private class Disposable : IDisposable
            {
                public Boolean Disposed { get; private set; }
                public void Dispose() { Disposed = true; }
            }
        }

        public class WhenGettingDistinctValues
        {
            [Fact]
            public void ReturnFirstInstanceOfKey()
            {
                var items = new[] { new Item { Key = "Key1" }, new Item { Key = "Key2" }, new Item { Key = "Key1" } };
                var distinct = items.Distinct(item => item.Key);

                Assert.Same(items[0], distinct.First());
            }

            [Fact]
            public void RemoveDuplicateKeys()
            {
                var items = new[] { new Item { Key = "Key1" }, new Item { Key = "Key2" }, new Item { Key = "Key1" }, new Item { Key = "Key3" } };
                var distinctItems = items.Distinct(item => item.Key).ToArray();

                Assert.Equal(3, distinctItems.Count());
                Assert.Same(items[0], distinctItems[0]);
                Assert.Same(items[1], distinctItems[1]);
                Assert.Same(items[3], distinctItems[2]);
            }

            private class Item
            {
                public String Key { get; set; }
            }
        }

        public class WhenEnsuringNotNull
        {
            [Fact]
            public void ReturnSameValueIfNotNull()
            {
                var expected = Enumerable.Empty<Object>();
                var actual = expected.EmptyIfNull();

                Assert.Same(expected, actual);
            }

            [Fact]
            public void ReturnEmptyIfNull()
            {
                Assert.Same(Enumerable.Empty<Object>(), default(IEnumerable<Object>).EmptyIfNull());
            }
        }
    }
}
#pragma warning restore 1720