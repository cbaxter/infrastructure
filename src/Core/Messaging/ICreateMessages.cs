﻿using System.Collections.Generic;

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

namespace Spark.Messaging
{

    /// <summary>
    /// Creates new instances of <see cref="Message{T}"/>.
    /// </summary>
    public interface ICreateMessages
    {
        /// <summary>
        /// Creates a new message with a payload of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="headers">The message headers.</param>
        /// <param name="payload">The message payload.</param>
        Message<T> Create<T>(IEnumerable<Header> headers, T payload);
    }
}
