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
    /// A message envelope containing a unique identifier, message headers and associated payload.
    /// </summary>
    public abstract class Message
    {
        private readonly Guid id;
        private readonly HeaderCollection headers;

        /// <summary>
        /// The unique message identifier.
        /// </summary>
        public Guid Id { get { return id; } }

        /// <summary>
        /// The set of message headers for this message.
        /// </summary>
        public HeaderCollection Headers { get { return headers; } }

        /// <summary>
        /// Get the underlying payload <see cref="Type"/>.
        /// </summary>
        public abstract Type PayloadType { get; }

        /// <summary>
        /// Initalizes a new instance of <see cref="Message"/>.
        /// </summary>
        internal Message(Guid id, HeaderCollection headers)
        {
            Verify.NotEqual(Guid.Empty, id, nameof(id));
            Verify.NotNull(headers, nameof(headers));

            this.id = id;
            this.headers = headers;
        }

        /// <summary>
        /// Get the message payload.
        /// </summary>
        /// <returns></returns>
        protected internal abstract Object GetPayload();

        /// <summary>
        /// Creates a new instance of <see cref="Message{TPayload}"/>.
        /// </summary>
        /// <param name="id">The unique message identifier</param>
        /// <param name="headers">The set of headers associated with this message.</param>
        /// <param name="payload">The message payload.</param>
        public static Message<TPayload> Create<TPayload>(Guid id, HeaderCollection headers, TPayload payload)
        {
            return new Message<TPayload>(id, headers, payload);
        }
    }

    /// <summary>
    /// A message envelope containing a unique identifier, message headers and associated payload.
    /// </summary>
    [Serializable]
    public sealed class Message<TPayload> : Message
    {
        private readonly TPayload payload;

        /// <summary>
        /// The message payload.
        /// </summary>
        public TPayload Payload { get { return payload; } }

        /// <summary>
        /// The payload type carried by this <see cref="Message"/>.
        /// </summary>
        public override Type PayloadType { get { return typeof(TPayload); } }

        /// <summary>
        /// Initializes a new instance of <see cref="Message{TPayload}"/>.
        /// </summary>
        /// <param name="id">The unique message identifier</param>
        /// <param name="headers">The set of headers associated with this message.</param>
        /// <param name="payload">The message payload.</param>
        public Message(Guid id, HeaderCollection headers, TPayload payload)
            : base(id, headers)
        {
            Verify.NotNull((Object)payload, nameof(payload));

            this.payload = payload;
        }

        /// <summary>
        /// Get the message payload.
        /// </summary>
        /// <returns></returns>
        protected internal override Object GetPayload()
        {
            return Payload;
        }

        /// <summary>
        /// Returns the description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} - {1}", Id, Payload);
        }
    }
}
