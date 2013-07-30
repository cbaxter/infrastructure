using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Spark.Cqrs.Commanding;

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

namespace Spark.Cqrs.Domain.Mappings
{
    /// <summary>
    /// A read-only mapping of aggregate type to command handler methods.
    /// </summary>
    public sealed class HandleMethodCollection : ReadOnlyDictionary<Type, Action<Aggregate, Command>>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="HandleMethodCollection"/>.
        /// </summary>
        /// <param name="dictionary">The underlying dictionary map of command type to aggregate handle method.</param>
        public HandleMethodCollection(IDictionary<Type, Action<Aggregate, Command>> dictionary)
            : base(dictionary)
        { }
    }
}
