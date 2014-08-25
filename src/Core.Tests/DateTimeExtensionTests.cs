using System;
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

namespace Test.Spark
{

    namespace UsingDateTimeExtensions
    {
        public class WhenAssumingUniversalTime
        {
            [Fact]
            public void ChangeDateTimeKindToUtcIfUnspecified()
            {
                DateTime? now = DateTime.UtcNow;

                Assert.Equal(now, new DateTime(now.Value.Ticks, DateTimeKind.Unspecified).AssumeUniversalTime());
            }

            [Fact]
            public void DoNotChangeLocalDateTimeKind()
            {
                DateTime? now = DateTime.Now;

                Assert.Equal(now, now.AssumeUniversalTime());
            }

            [Fact]
            public void DoNotChangeUtcDateTimeKind()
            {
                DateTime? now = DateTime.UtcNow;

                Assert.Equal(now, now.AssumeUniversalTime());
            }

            [Fact]
            public void IgnoreNullValues()
            {
                DateTime? now = null;

                Assert.Null(now.AssumeUniversalTime());
            }
        }

        public class WhenAssumingLocalTime
        {
            [Fact]
            public void ChangeDateTimeKindToLocalIfUnspecified()
            {
                DateTime? now = DateTime.Now;

                Assert.Equal(now, new DateTime(now.Value.Ticks, DateTimeKind.Unspecified).AssumeLocalTime());
            }

            [Fact]
            public void DoNotChangeLocalDateTimeKind()
            {
                DateTime? now = DateTime.Now;

                Assert.Equal(now, now.AssumeLocalTime());
            }

            [Fact]
            public void DoNotChangeUtcDateTimeKind()
            {
                DateTime? now = DateTime.UtcNow;

                Assert.Equal(now, now.AssumeLocalTime());
            }

            [Fact]
            public void IgnoreNullValues()
            {
                DateTime? now = null;

                Assert.Null(now.AssumeLocalTime());
            }
        }

        public class WhenConvertingNullableDateTimeToLocalTime
        {
            [Fact]
            public void DelegateToDateTimeToLocalTimeIfNotNull()
            {
                DateTime? now = DateTime.UtcNow;

                Assert.Equal(now.Value.ToLocalTime(), now.ToLocalTime());
            }

            [Fact]
            public void IgnoreNullValues()
            {
                DateTime? now = null;

                Assert.Null(now.ToLocalTime());
            }
        }

        public class WhenConvertingNullableDateTimeToUtcTime
        {
            [Fact]
            public void DelegateToDateTimeToLocalTimeIfNotNull()
            {
                DateTime? now = DateTime.Now;

                Assert.Equal(now.Value.ToUniversalTime(), now.ToUniversalTime());
            }

            [Fact]
            public void IgnoreNullValues()
            {
                DateTime? now = null;

                Assert.Null(now.ToUniversalTime());
            }
        }
    }
}
