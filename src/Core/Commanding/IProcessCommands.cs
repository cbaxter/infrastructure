using System;
using Spark.Infrastructure.Messaging;

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

namespace Spark.Infrastructure.Commanding
{
    /// <summary>
    /// <see cref="Command"/> processor instance.
    /// </summary>
    public interface IProcessCommands
    {
        /// <summary>
        /// Processes a given <see cref="Command"/> instance.
        /// </summary>
        /// <param name="commandId">The unique <see cref="Command"/> instance id.</param>
        /// <param name="headers">The message headers associated with the <paramref name="envelope"/>.</param>
        /// <param name="envelope">The <see cref="Command"/> envelope to process.</param>
        void Process(Guid commandId, HeaderCollection headers, CommandEnvelope envelope);
    }
}
