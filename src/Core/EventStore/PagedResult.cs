using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spark.Resources;

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
    /// A paged enumerable for returning paged results.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedResult<T> : IEnumerable<T>
    {
        private readonly Func<T, Page, IEnumerable<T>> pageRetriever;
        private readonly Int64 pageSize;

        /// <summary>
        /// Initializes a new instance of <see cref="PagedResult{T}"/>.
        /// </summary>
        /// <param name="pageSize">The page size.</param>
        /// <param name="pageRetriever">The paging function.</param>
        public PagedResult(Int64 pageSize, Func<T, Page, IEnumerable<T>> pageRetriever)
        {
            Verify.NotNull(pageRetriever, "pageRetriever");
            Verify.GreaterThan(0, pageSize, "pageSize");

            this.pageRetriever = pageRetriever;
            this.pageSize = pageSize;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the paged collection.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            var count = 0;
            var lastResult = default(T);
            var currentPage = new Page(0, pageSize);

            do
            {
                var results = pageRetriever(lastResult, currentPage) ?? Enumerable.Empty<T>();

                count = 0;
                foreach (var result in results)
                {
                    if (++count > pageSize)
                        throw new InvalidOperationException(Exceptions.PageSizeExceeded.FormatWith(pageSize));

                    yield return lastResult = result;
                }

                currentPage = currentPage.NextPage();
            } while (count == pageSize);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the paged collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
