﻿using System;
using System.Runtime.Serialization;

/* Copyright (c) 2013 Spark Software Ltd.
 * 
 * This source is subject to the GNU Lesser General Public License.
 * See: http://www.gnu.org/copyleft/lesser.html
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE. 
 */

namespace Spark.Cqrs.Domain
{
    /// <summary>
    /// Represents errors that occur when an aggregate definition is invalid.
    /// </summary>
    [Serializable]
    public class MappingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MappingException"/> with the default message.
        /// </summary>
        public MappingException()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="MappingException"/> with a custom <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MappingException(String message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="MappingException"/> with a custom <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused the current exception.</param>
        public MappingException(String message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected MappingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
