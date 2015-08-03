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
    /// Internal structure used to serializer/deserialize headers and events as a single unit.
    /// </summary>
    [Serializable]
    internal struct CommitData
    {
        public static readonly CommitData Empty = new CommitData(HeaderCollection.Empty, EventCollection.Empty);
        public readonly HeaderCollection Headers;
        public readonly EventCollection Events;

        /// <summary>
        /// Initializes a new instance of <see cref="CommitData"/>
        /// </summary>
        /// <param name="headers">The headers associated with this commit.</param>
        /// <param name="events">The events associated with this commit.</param>
        public CommitData(HeaderCollection headers, EventCollection events)
        {
            Verify.NotNull(headers, "headers");
            Verify.NotNull(events, "events");

            Headers = headers;
            Events = events;
        }
    }
}
