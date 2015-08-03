using System;
using System.Collections.Generic;
using System.Linq;

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

namespace Spark
{
    /// <summary>
    /// Determine whether an exception is a transient error that may allow an operation to be retried.
    /// </summary>
    public interface IDetectTransientErrors
    {
        /// <summary>
        /// Returns <value>true</value> if the <see cref="Exception"/> <paramref name="ex"/> is a transient error; otherwise returns <value>false</value>.
        /// </summary>
        /// <param name="ex">The exception to check if represents a transient error.</param>
        Boolean IsTransient(Exception ex);
    }

    /// <summary>
    /// Determine whether an exception is a transient error that may allow an operation to be retried.
    /// </summary>
    internal sealed class TransientErrorRegistry : IDetectTransientErrors
    {
        private readonly IReadOnlyList<IDetectTransientErrors> transientErrorDetectors;

        /// <summary>
        /// Initializes a new instance of <see cref="TransientErrorRegistry"/>.
        /// </summary>
        /// <param name="transientErrorDetectors"></param>
        public TransientErrorRegistry(IEnumerable<IDetectTransientErrors> transientErrorDetectors)
        {
            this.transientErrorDetectors = transientErrorDetectors == null ? new IDetectTransientErrors[0] : transientErrorDetectors.AsReadOnly();
        }

        /// <summary>
        /// Returns <value>true</value> if the <see cref="Exception"/> <paramref name="ex"/> is a transient error; otherwise returns <value>false</value>.
        /// </summary>
        /// <param name="ex">The exception to check if represents a transient error.</param>
        public Boolean IsTransient(Exception ex)
        {
            return transientErrorDetectors.Any(t => t.IsTransient(ex));
        }
    }
}
