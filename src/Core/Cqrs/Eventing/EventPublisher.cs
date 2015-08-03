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

namespace Spark.Cqrs.Eventing
{
    /// <summary>
    /// Publishes events on the underlying <see cref="Event"/> message bus.
    /// </summary>
    public sealed class EventPublisher : IPublishEvents
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly ISendMessages<EventEnvelope> messageSender;
        private readonly ICreateMessages messageFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="EventPublisher"/>.
        /// </summary>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="messageSender">The message sender.</param>
        public EventPublisher(ICreateMessages messageFactory, ISendMessages<EventEnvelope> messageSender)
        {
            Verify.NotNull(messageFactory, "messageFactory");
            Verify.NotNull(messageSender, "messageSender");

            this.messageFactory = messageFactory;
            this.messageSender = messageSender;
        }

        /// <summary>
        /// Publishes the specified <paramref name="payload"/> on the underlying message bus.
        /// </summary>
        /// <param name="headers">The set of message headers associated with the event.</param>
        /// <param name="payload">The event payload to be published.</param>
        public void Publish(IEnumerable<Header> headers, EventEnvelope payload)
        {
            Verify.NotNull(payload, "payload");

            Log.TraceFormat("Publishing {0} from {1}", payload.Event, payload.AggregateId);

            messageSender.Send(messageFactory.Create(headers, payload));
        }
    }
}
