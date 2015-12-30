using System;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Spark.Messaging
{
    /// <summary>
    /// An inline message bus for use by single-process applications when the result must be processed immediately to ensure messages are not lost.
    /// </summary>
    public sealed class DirectMessageSender<T> : ISendMessages<T>
    {
        private readonly IProcessMessages<T> messageProcessor;

        /// <summary>
        /// Initializes a new instance of <see cref="DirectMessageSender{T}"/> using the specified <see cref="IProcessMessages{T}"/> instance.
        /// </summary>
        /// <param name="messageProcessor">The message processor.</param>
        public DirectMessageSender(IProcessMessages<T> messageProcessor)
        {
            Verify.NotNull(messageProcessor, nameof(messageProcessor));

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
                messageProcessor.Process(message);
            }
            catch (AggregateException ex)
            {
                throw ex.Flatten().InnerException;
            }
        }
    }
}
