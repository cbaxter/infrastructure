using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Spark.Eventing;

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

namespace Spark.Domain.Mappings
{
    /// <summary>
    /// A read-only mapping of aggregate type to event apply methods.
    /// </summary>
    public sealed class ApplyMethodCollection : ReadOnlyDictionary<Type, Action<Aggregate, Event>>
    {
        private readonly Boolean applyOptional;

        /// <summary>
        /// True if an event apply method is optional; otherwise false.
        /// </summary>
        public Boolean ApplyOptional { get { return applyOptional; } }

        /// <summary>
        /// Initializes a new instance of <see cref="ApplyMethodCollection"/>.
        /// </summary>
        /// <param name="applyOptional">Flag indicating if an exception should be thrown if an event apply method is not found.</param>
        /// <param name="dictionary">The underlying dictionary map of event type to aggregate apply method.</param>
        public ApplyMethodCollection(Boolean applyOptional, IDictionary<Type, Action<Aggregate, Event>> dictionary)
            : base(dictionary)
        {
            this.applyOptional = applyOptional;
        }
    }
}
