using System;
using Spark.Cqrs.Eventing;
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

namespace Spark.EventStore
{
    /// <summary>
    /// Represents a collection of events associated with a single unit of work.
    /// </summary>
    public class Commit
    {
        /// <summary>
        /// The global commit sequence associated with this commit.
        /// </summary>
        public Int64? Id { get; internal set; }

        /// <summary>
        /// Gets the time when the commit was persisted to the database.
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// The unique identifier associated with this commit.
        /// </summary>
        public Guid CorrelationId { get; private set; }

        /// <summary>
        /// The stream identifier associated with this commit.
        /// </summary>
        public Guid StreamId { get; private set; }

        /// <summary>
        /// Gets the current version of the event stream.
        /// </summary>
        public Int32 Version { get; private set; }

        /// <summary>
        /// Gets the set of commit headers associated with this commit.
        /// </summary>
        public HeaderCollection Headers { get; private set; }

        /// <summary>
        /// Gets the set of events associated with this commit.
        /// </summary>
        public EventCollection Events { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Commit"/>.
        /// </summary>
        /// <param name="correlationId">The correlation id.</param>
        /// <param name="streamId">The event stream id.</param>
        /// <param name="version">The event stream revision.</param>
        /// <param name="headers">The optional set of headers associated with this commit.</param>
        /// <param name="events">The optional set of events associated with this commit.</param>
        public Commit(Guid correlationId, Guid streamId, Int32 version, HeaderCollection headers, EventCollection events)
            : this(null, SystemTime.Now, correlationId, streamId, version, headers, events)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="Commit"/>.
        /// </summary>
        /// <param name="id">The unique commit id.</param>
        /// <param name="timestamp">The <see cref="DateTime"/> when the snapshot was persisted.</param>
        /// <param name="correlationId">The correlation id.</param>
        /// <param name="streamId">The event stream id.</param>
        /// <param name="version">The event stream revision.</param>
        /// <param name="headers">The optional set of headers associated with this commit.</param>
        /// <param name="events">The optional set of events associated with this commit.</param>
        internal Commit(Int64? id, DateTime timestamp, Guid correlationId, Guid streamId, Int32 version, HeaderCollection headers, EventCollection events)
        {
            Verify.NotEqual(Guid.Empty, correlationId, nameof(correlationId));
            Verify.NotEqual(Guid.Empty, streamId, nameof(streamId));
            Verify.GreaterThan(0, version, nameof(version));

            Id = id;
            Timestamp = timestamp;
            CorrelationId = correlationId;
            StreamId = streamId;
            Version = version;
            Events = events ?? EventCollection.Empty;
            Headers = headers ?? HeaderCollection.Empty;
        }

        /// <summary>
        /// Returns the description for this instance.
        /// </summary>
        public override string ToString()
        {
            return $"{GetType()} - {Id}";
        }
    }
}
