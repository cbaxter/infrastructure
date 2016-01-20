using System.Collections.Generic;
using Spark.Logging;
using Spark.Messaging;

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

namespace Spark.Cqrs.Commanding
{
    /// <summary>
    /// Publishes commands on the underlying <see cref="Command"/> message bus.
    /// </summary>
    public sealed class CommandPublisher : IPublishCommands
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly ISendMessages<CommandEnvelope> messageSender;
        private readonly ICreateMessages messageFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="CommandPublisher"/>.
        /// </summary>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="messageSender">The message sender.</param>
        public CommandPublisher(ICreateMessages messageFactory, ISendMessages<CommandEnvelope> messageSender)
        {
            Verify.NotNull(messageFactory, nameof(messageFactory));
            Verify.NotNull(messageSender, nameof(messageSender));

            this.messageFactory = messageFactory;
            this.messageSender = messageSender;
        }

        /// <summary>
        /// Publishes the specified <paramref name="payload"/> on the underlying message bus.
        /// </summary>
        /// <param name="headers">The set of message headers associated with the command.</param>
        /// <param name="payload">The command payload to be published.</param>
        public void Publish(IEnumerable<Header> headers, CommandEnvelope payload)
        {
            Verify.NotNull(payload, nameof(payload));

            Log.Trace("Publishing {0} to {1}", payload.Command, payload.AggregateId);

            messageSender.Send(messageFactory.Create(headers, payload));
        }
    }
}
