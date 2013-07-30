using System;
using System.Collections.Generic;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Messaging;

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
    /// Represents a defered saga command to be published once saga state has been updated successfully.
    /// </summary>
    internal sealed class SagaCommand
    {
        private readonly IEnumerable<Header> headers;
        private readonly Guid aggregateId;
        private readonly Command command;

        /// <summary>
        /// The set of custom message headers associated with the <see cref="Command"/>.
        /// </summary>
        public IEnumerable<Header> Headers { get { return headers; } }
        
        /// <summary>
        /// The <see cref="Aggregate"/> identifier that will handle the specified <see cref="Command"/>.
        /// </summary>
        public Guid AggregateId { get { return aggregateId; } }

        /// <summary>
        /// The command to be published.
        /// </summary>
        public Command Command { get { return command; } }

        /// <summary>
        /// Initializes a new instance of <see cref="SagaCommand"/>.
        /// </summary>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="command"/>.</param>
        /// <param name="command">The command to be published.</param>
        /// <param name="headers">The set of custom message headers associated with the <paramref name="command"/>.</param>
        public SagaCommand(Guid aggregateId, IEnumerable<Header> headers, Command command)
        {
            this.aggregateId = aggregateId;
            this.headers = headers;
            this.command = command;
        }
    }
}
