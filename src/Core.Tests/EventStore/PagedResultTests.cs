using System;
using System.Collections.Generic;
using System.Linq;
using Spark.EventStore;
using Spark.Resources;
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

namespace Spark.Tests.EventStore
{
    public static class UsingPagedResult
    {
        // ReSharper disable NotResolvedInText
        public class WhenCreatingPagedResult
        {
            [Fact]
            public void PageSizeMustBeGreaterThanZero()
            {
                var expectedEx = new ArgumentOutOfRangeException("pageSize", 0, Exceptions.ArgumentNotGreaterThanValue.FormatWith(0));
                var actualEx = Assert.Throws<ArgumentOutOfRangeException>(() => new PagedResult<Object>(0, (lastResult, page) => Enumerable.Empty<Object>()));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }

            [Fact]
            public void PageRetrieverCannotBeNull()
            {
                var expectedEx = new ArgumentNullException("pageRetriever");
                var actualEx = Assert.Throws<ArgumentNullException>(() => new PagedResult<Object>(10, null));

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }
        }
        // ReSharper restore NotResolvedInText

        public class WhenEnumeratingResults
        {
            [Fact]
            public void PageRetrieverCanReturnNullSafely()
            {
                Assert.DoesNotThrow(() => new PagedResult<Object>(10, (lastResult, page) => null).ToList());
            }

            [Fact]
            public void PageRetrieverMustReturnNoMoreThanPageSize()
            {
                var expectedEx = new InvalidOperationException(Exceptions.PageSizeExceeded.FormatWith(10));
                var actualEx = Assert.Throws<InvalidOperationException>(() => new PagedResult<Int32>(10, (lastResult, page) => Enumerable.Repeat(1, 11)).ToList());

                Assert.Equal(expectedEx.Message, actualEx.Message);
            }

            [Fact]
            public void PageRetrieverNotCalledIfLastPageNotFull()
            {
                var pageQueue = new Queue<IEnumerable<Int32>>();

                pageQueue.Enqueue(Enumerable.Repeat(1, 9));

                Assert.Equal(9, new PagedResult<Int32>(10, (lastResult, page) => pageQueue.Dequeue()).Count());
                Assert.Equal(0, pageQueue.Count);
            }

            [Fact]
            public void PageRetrieverCalledIfLastPageFull()
            {
                var pageQueue = new Queue<IEnumerable<Int32>>();

                pageQueue.Enqueue(Enumerable.Repeat(1, 10));
                pageQueue.Enqueue(Enumerable.Empty<Int32>());

                Assert.Equal(10, new PagedResult<Int32>(10, (lastResult, page) => pageQueue.Dequeue()).Count());
                Assert.Equal(0, pageQueue.Count);
            }

            [Fact]
            public void CanUseNonGenericEnumerator()
            {
                var pageQueue = new Queue<IEnumerable<Int32>>();

                pageQueue.Enqueue(Enumerable.Repeat(1, 1));

                Assert.Equal(1, new PagedResult<Int32>(10, (lastResult, page) => pageQueue.Dequeue()).Cast<Object>().Count());
                Assert.Equal(0, pageQueue.Count);
            }
        }
    }
}
