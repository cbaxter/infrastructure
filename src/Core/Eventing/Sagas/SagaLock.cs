using System;

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

namespace Spark.Infrastructure.Eventing.Sagas
{
    /// <summary>
    /// A <see cref="Saga"/> synchronization lock object.
    /// </summary>
    internal sealed class SagaLock
    {
        private Int32 referenceCount;

        /// <summary>
        /// Increment the current saga reference count.
        /// </summary>
        internal Int32 Increment()
        {
            return ++referenceCount;
        }

        /// <summary>
        /// Decrement the current saga reference count.
        /// </summary>
        internal Int32 Decrement()
        {
            return --referenceCount;
        }
    }
}
