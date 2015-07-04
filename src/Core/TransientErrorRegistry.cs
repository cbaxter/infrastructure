using System;
using System.Collections.Generic;
using System.Linq;

/* Copyright (c) 2014 Spark Software Ltd.
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

namespace Spark
{
    /// <summary>
    /// Determine whether an exception is a transient error that may allow an operation to be retried.
    /// </summary>
    public interface IDetectTransientErrors
    {
        /// <summary>
        /// Returns <value>true</value> if the <see cref="Exception"/> <paramref name="ex"/> is a transient error; otherwise returns <value>false</value>.
        /// </summary>
        /// <param name="ex">The exception to check if represents a transient error.</param>
        Boolean IsTransient(Exception ex);
    }

    /// <summary>
    /// Determine whether an exception is a transient error that may allow an operation to be retried.
    /// </summary>
    internal sealed class TransientErrorRegistry : IDetectTransientErrors
    {
        private readonly IReadOnlyList<IDetectTransientErrors> transientErrorDetectors;

        /// <summary>
        /// Initializes a new instance of <see cref="TransientErrorRegistry"/>.
        /// </summary>
        /// <param name="transientErrorDetectors"></param>
        public TransientErrorRegistry(IEnumerable<IDetectTransientErrors> transientErrorDetectors)
        {
            this.transientErrorDetectors = transientErrorDetectors == null ? new IDetectTransientErrors[0] : transientErrorDetectors.AsReadOnly();
        }

        /// <summary>
        /// Returns <value>true</value> if the <see cref="Exception"/> <paramref name="ex"/> is a transient error; otherwise returns <value>false</value>.
        /// </summary>
        /// <param name="ex">The exception to check if represents a transient error.</param>
        public Boolean IsTransient(Exception ex)
        {
            return transientErrorDetectors.Any(t => t.IsTransient(ex));
        }
    }
}
