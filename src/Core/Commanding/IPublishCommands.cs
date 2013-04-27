﻿using System.Collections.Generic;
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
    /// Command publisher.
    /// </summary>
    public interface IPublishCommands
    {
        /// <summary>
        /// Publishes the specified <paramref name="command"/> on the underlying message bus.
        /// </summary>
        /// <param name="command">The command to be published.</param>
        /// <param name="headers">The set of message headers associated with the command.</param>
        void Publish(Command command, IEnumerable<Header> headers);
    }

    /// <summary>
    /// Extension methods of <see cref="IPublishCommands"/>
    /// </summary>
    public static class CommandPublisherExtensions
    {
        /// <summary>
        /// Publishes the specified <paramref name="command"/> with only the default message headers.
        /// </summary>
        /// <param name="publisher">The command publisher.</param>
        /// <param name="command">The command to be published.</param>
        public static void Publish(this IPublishCommands publisher, Command command)
        {
            Verify.NotNull(publisher, "publisher");

            publisher.Publish(command, null);
        }

        /// <summary>
        /// Publishes the specified <paramref name="command"/> with the set of custom message headers.
        /// </summary>
        /// <param name="publisher">The command publisher.</param>
        /// <param name="command">The command to be published.</param>
        /// <param name="headers">The set of one or more custom message headers.</param>
        public static void Publish(this IPublishCommands publisher, Command command, params Header[] headers)
        {
            Verify.NotNull(publisher, "publisher");

            publisher.Publish(command, headers);
        }
    }
}