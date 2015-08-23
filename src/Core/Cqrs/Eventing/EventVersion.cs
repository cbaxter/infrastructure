using System;
using Spark.Cqrs.Domain;

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
    /// Identifies a specific aggregate event instance.
    /// </summary>
    public struct EventVersion : IComparable<EventVersion>, IEquatable<EventVersion>
    {
        private readonly Int32 version;
        private readonly Int32 count;
        private readonly Int32 item;

        /// <summary>
        /// Represents an empty <see cref="EventVersion"/>. This field is read-only.
        /// </summary>
        public static readonly EventVersion Empty = new EventVersion();

        /// <summary>
        /// The <see cref="Aggregate"/> version.
        /// </summary>
        public Int32 Version { get { return version; } }

        /// <summary>
        /// The total number of events raised within the specific aggregate <see cref="Version"/>.
        /// </summary>
        public Int32 Count { get { return count; } }

        /// <summary>
        /// The event oridinal within the specific aggregate <see cref="Version"/>.
        /// </summary>
        public Int32 Item { get { return item; } }

        /// <summary>
        /// Initializes a new instance of <see cref="EventVersion"/>.
        /// </summary>
        /// <param name="version">The <see cref="Aggregate"/> version.</param>
        /// <param name="count">The total number of events raised within the specific aggregate <see cref="Version"/></param>
        /// <param name="item">The event oridinal within the specific aggregate <see cref="Version"/>.</param>
        public EventVersion(Int32 version, Int32 count, Int32 item)
        {
            Verify.GreaterThan(0, version, nameof(version));
            Verify.GreaterThanOrEqual(0, count, nameof(count));
            Verify.GreaterThanOrEqual(0, item, nameof(item));
            Verify.LessThanOrEqual(count, item, nameof(item));

            this.version = version;
            this.count = count;
            this.item = item;
        }
        
        /// <summary>
        /// Compares this instance to a specified <see cref="EventVersion"/> and returns an indication of their relative values.
        /// </summary>
        /// <param name="value">An <see cref="EventVersion"/> to compare.</param>
        public Int32 CompareTo(EventVersion value)
        {
            return version == value.version ? item.CompareTo(value.item) : version.CompareTo(value.version);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="Object"/> are equal.
        /// </summary>
        /// <param name="other">Another object to compare.</param>
        public override Boolean Equals(Object other)
        {
            return other is EventVersion && Equals((EventVersion)other);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="EventVersion"/> are equal.
        /// </summary>
        /// <param name="other">Another <see cref="EventVersion"/> to compare.</param>
        public Boolean Equals(EventVersion other)
        {
            return version == other.version && count == other.count && item == other.item;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {
                var hash = 43;

                hash = (hash * 397) + Version.GetHashCode();
                hash = (hash * 397) + Count.GetHashCode();
                hash = (hash * 397) + Item.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns the page description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Format("{0} (Event {1} of {2})", Version, Item, Count);
        }
    }
}
