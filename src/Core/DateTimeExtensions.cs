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

namespace Spark
{
    /// <summary>
    /// Extension methods of <see cref="DateTime"/>.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts the value of the current <see cref="DateTime"/> object to Coordinated Universal Time (UTC) if <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Unspecified"/>.
        /// </summary>
        /// <param name="value">The date time value to convert if required. </param>
        public static DateTime AssumeUniversalTime(this DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified ? new DateTime(value.Ticks, DateTimeKind.Utc) : value;
        }

        /// <summary>
        /// Converts the value of the current <see cref="DateTime"/> object to Coordinated Universal Time (UTC) if <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Unspecified"/>.
        /// </summary>
        /// <param name="value">The date time value to convert if required. </param>
        public static DateTime? AssumeUniversalTime(this DateTime? value)
        {
            return value.HasValue ? value.Value.AssumeUniversalTime() : default(DateTime?);
        }

        /// <summary>
        /// Converts the value of the current <see cref="DateTime"/> object to local time if <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Unspecified"/>.
        /// </summary>
        /// <param name="value">The date time value to convert if required. </param>
        public static DateTime AssumeLocalTime(this DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified ? new DateTime(value.Ticks, DateTimeKind.Local) : value;
        }

        /// <summary>
        /// Converts the value of the current <see cref="DateTime"/> object to local time if <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Unspecified"/>.
        /// </summary>
        /// <param name="value">The date time value to convert if required. </param>
        public static DateTime? AssumeLocalTime(this DateTime? value)
        {
            return value.HasValue ? value.Value.AssumeLocalTime() : default(DateTime?);
        }

        /// <summary>
        /// Converts the value of the current <see cref="DateTime"/> object to local time.
        /// </summary>
        /// <param name="value">The date time value to convert if required. </param>
        public static DateTime? ToLocalTime(this DateTime? value)
        {
            return value.HasValue ? value.Value.ToLocalTime() : default(DateTime?);
        }

        /// <summary>
        /// Converts the value of the current <see cref="DateTime"/> object to Coordinated Universal Time (UTC).
        /// </summary>
        /// <param name="value">The date time value to convert if required. </param>
        public static DateTime? ToUniversalTime(this DateTime? value)
        {
            return value.HasValue ? value.Value.ToUniversalTime() : default(DateTime?);
        }
    }
}
