using System;
using System.Collections.Generic;

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

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// Data access contract for a saga store.
    /// </summary>
    public interface IStoreSagas : IDisposable
    {
        /// <summary>
        /// Creates a new saga instance identified by the specified <paramref name="type"/> and <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The type of saga to be retrieved.</param>
        /// <param name="id">The correlation id of the saga to be retrieved.</param>
        Saga CreateSaga(Type type, Guid id);

        /// <summary>
        /// Attempt to retrieve an existing saga instance identified by the specified <paramref name="type"/> and <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The type of saga to be retrieved.</param>
        /// <param name="id">The correlation id of the saga to be retrieved.</param>
        /// <param name="saga">The <see cref="Saga"/> instance if found; otherwise <value>null</value>.</param>
        Boolean TryGetSaga(Type type, Guid id, out Saga saga);

        /// <summary>
        /// Get all scheduled saga timeouts before the specified maximum timeout.
        /// </summary>
        /// <param name="maximumTimeout">The exclusive timeout upper bound.</param>
        IReadOnlyList<SagaTimeout> GetScheduledTimeouts(DateTime maximumTimeout);

        /// <summary>
        /// Save the specified <paramref name="context"/> changes for the given <paramref name="saga"/>.
        /// </summary>
        /// <param name="saga">The current saga version for which the context applies.</param>
        /// <param name="context">The saga context containing the saga changes to be applied.</param>
        void Save(Saga saga, SagaContext context);
        
        /// <summary>
        /// Deletes all existing sagas from the saga store.
        /// </summary>
        void Purge();
    }
}
