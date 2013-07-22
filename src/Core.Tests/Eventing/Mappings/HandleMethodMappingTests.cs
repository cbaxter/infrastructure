using System;
using Moq;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Eventing.Mappings;
using Spark.Infrastructure.Resources;
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

namespace Spark.Infrastructure.Tests.Eventing.Mappings
{
    public static class HandleMethodMappingTests
    {
        public class WhenCreatingMapping
        {
            [Fact]
            public void EventTypeMustDeriveFromEvent()
            {
                // ReSharper disable NotResolvedInText
                var mapping = new ObjectEventTypeMapping();
                var expectedEx = new ArgumentException(Exceptions.TypeDoesNotDeriveFromBase.FormatWith(typeof(Event), typeof(Object)), "eventType");
                var ex = Assert.Throws<ArgumentException>(() => mapping.GetMappings(new Mock<IServiceProvider>().Object));

                Assert.Equal(expectedEx.Message, ex.Message);
                // ReSharper restore NotResolvedInText
            }

            protected class ObjectEventTypeMapping : HandleMethodMapping
            {
                protected override void RegisterMappings(HandleMethodMappingBuilder builder)
                {
                    builder.Register(typeof(Object), (handler, @event) => { throw new Exception(); });
                }
            }
        }
    }
}
