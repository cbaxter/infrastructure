using System;
using System.Collections.Generic;

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
    /// Data access contract for an event store.
    /// </summary>
    public interface IRetrieveEvents
    {
        /// <summary>
        /// Get all undispatched commits.
        /// </summary>
        IEnumerable<Commit> GetUndispatched();

        /// <summary>
        /// Get all known stream identifiers.
        /// </summary>
        /// <remarks>This method is not safe to call on an active event store; only use when new streams are not being committed.</remarks>
        IEnumerable<Guid> GetStreams();

        /// <summary>
        /// Get the specified commit sequence range starting after <paramref name="skip"/> and returing <paramref name="take"/> commits.
        /// </summary>
        /// <param name="skip">The commit sequence lower bound (exclusive).</param>
        /// <param name="take">The number of commits to include in the result.</param>
        IReadOnlyList<Commit> GetRange(Int64 skip, Int64 take);

        /// <summary>
        /// Gets all commits for the specified <paramref name="streamId"/> with a version greater than or equal to <paramref name="minimumVersion"/>.
        /// </summary>
        /// <param name="streamId">The unique stream identifier.</param>
        /// <param name="minimumVersion">The minimum stream version (inclusive).</param>
        IEnumerable<Commit> GetStream(Guid streamId, Int32 minimumVersion);
    }
}
