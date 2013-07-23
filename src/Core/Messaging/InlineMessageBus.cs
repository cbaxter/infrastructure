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

namespace Spark.Infrastructure.Messaging
{
    /// <summary>
    /// An inline message bus for use by single-process applications when the result must be processed immediately to ensure messages are not lost.
    /// </summary>
    public sealed class InlineMessageBus<T> : ISendMessages<T>
    {
        private readonly IProcessMessages<T> messageProcessor;

        /// <summary>
        /// Initializes a new instance of <see cref="InlineMessageBus{T}"/> using the specified <see cref="IProcessMessages{T}"/> instance.
        /// </summary>
        /// <param name="messageProcessor">The message processor.</param>
        public InlineMessageBus(IProcessMessages<T> messageProcessor)
        {
            Verify.NotNull(messageProcessor, "messageProcessor");

            this.messageProcessor = messageProcessor;
        }

        /// <summary>
        /// Publishes a message on the underlying message bus.
        /// </summary>
        /// <param name="message">The message to publish on the underlying message bus.</param>
        public void Send(Message<T> message)
        {
            try
            {
                messageProcessor.ProcessAsync(message).Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.Flatten().InnerException; 
            }
        }
    }
}
