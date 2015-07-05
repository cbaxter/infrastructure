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

namespace Spark.EventStore
{
    /// <summary>
    /// Represents a point in time snapshot of an event stream.
    /// </summary>
    public sealed class Snapshot
    {
        /// <summary>
        /// The stream identifier associated with this snapshot.
        /// </summary>
        public Guid StreamId { get; private set; }

        /// <summary>
        /// Gets the event stream version associated with this snapshot.
        /// </summary>
        public Int32 Version { get; private set; }

        /// <summary>
        /// Gets the snapshot state.
        /// </summary>
        public Object State { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Snapshot"/>.
        /// </summary>
        /// <param name="streamId">The stream id.</param>
        /// <param name="version">The snapshot version.</param>
        /// <param name="state">The snapshot state.</param>
        public Snapshot(Guid streamId, Int32 version, Object state)
        { 
            Verify.NotEqual(Guid.Empty, streamId, "streamId");
            Verify.GreaterThan(0, version, "version");
            Verify.NotNull(state, "state");

            StreamId = streamId;
            Version = version;
            State = state;
        }
    }
}
