using System;
using System.Collections.Generic;

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

namespace Spark.Infrastructure
{
    /// <summary>
    /// Represents an object whose internal state may be captured and externalized without violating encapsulation.
    /// </summary>
    public abstract class StateObject
    {
        /// <summary>
        /// Get the <see cref="StateObject"/> field type for the specified attribute <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        protected internal virtual Type GetFieldType(String name)
        {
            var type = ObjectMapper.GetFieldType(GetType(), name);

            return type;
        }

        /// <summary>
        /// Get the underlying <see cref="StateObject"/> mutable state information.
        /// </summary>
        protected internal virtual IDictionary<String, Object> GetState()
        {
            var state = ObjectMapper.GetState(this);

            return state;
        }

        /// <summary>
        /// Set the underlying <see cref="StateObject"/> mutable state information.
        /// </summary>
        /// <param name="state">The state dictionary to be mapped to this entity instance.</param>
        protected internal virtual void SetState(IDictionary<String, Object> state)
        {
            ObjectMapper.SetState(this, state);
        }
    }
}
