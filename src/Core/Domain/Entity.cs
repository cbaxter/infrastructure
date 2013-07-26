using System;
using System.Runtime.Serialization;
using Spark.Commanding;
using Spark.Eventing;
using Spark.Resources;

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

namespace Spark.Domain
{
    /// <summary>
    /// A uniquely identifiable <see cref="Object"/> within a given <see cref="Aggregate"/> root.
    /// </summary>
    public abstract class Entity : StateObject
    {
        protected static class Property
        {
            public const String Id = "id";
        }

        /// <summary>
        /// The unique entity identifier.
        /// </summary>
        [DataMember(Name = Property.Id)]
        public Guid Id { get; internal set; }

        /// <summary>
        /// Raises the specified event.
        /// </summary>
        /// <param name="e">The <see cref="Event"/> to be raised.</param>
        protected void Raise(Event e)
        {
            Verify.NotNull(e, "e");

            var context = CommandContext.Current;
            if (context == null)
                throw new InvalidOperationException(Exceptions.NoCommandContext);

            context.Raise(e);
        }
    }
}
