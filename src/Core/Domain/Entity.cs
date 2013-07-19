using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Resources;

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

namespace Spark.Infrastructure.Domain
{
    /// <summary>
    /// A uniquely identifiable <see cref="Object"/> within a given <see cref="Aggregate"/> root.
    /// </summary>
    public abstract class Entity
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

        /// <summary>
        /// Get the <see cref="Entity"/> field type for the specified attribute <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        protected internal virtual Type GetFieldType(String name)
        {
            var type = ObjectMapper.GetFieldType(GetType(), name);

            return type;
        }

        /// <summary>
        /// Get the underlying <see cref="Entity"/> mutable state information.
        /// </summary>
        protected internal virtual IDictionary<String, Object> GetState()
        {
            var state = ObjectMapper.GetState(this);

            return state;
        }

        /// <summary>
        /// Set the underlying <see cref="Entity"/> mutable state information.
        /// </summary>
        /// <param name="state">The state dictionary to be mapped to this entity instance.</param>
        protected internal virtual void SetState(IDictionary<String, Object> state)
        {
            ObjectMapper.SetState(this, state);
        }
    }
}
