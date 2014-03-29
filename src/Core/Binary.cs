using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/* Copyright (c) 2014 Spark Software Ltd.
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

namespace Spark
{
    /// <summary>
    /// An immutable <see cref="T:Byte[]"/> utility wrapper wrapper for strongly typed byte array references.
    /// </summary>
    public class Binary : IComparable, IComparable<Binary>, IComparable<Byte[]>, IEquatable<Binary>, IEquatable<Byte[]>, IEnumerable<Byte>
    {
        private readonly Byte[] data;

        /// <summary>
        /// Gets an empty <see cref="Binary"/> reference.
        /// </summary>
        public static readonly Binary Empty = new Binary(new Byte[0]);

        /// <summary>
        /// Gets the byte value at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index"></param>
        public Byte this[Int32 index] { get { return data[index]; } }

        /// <summary>
        /// Gets the total number of bytes contained within this <see cref="Binary"/> instance.
        /// </summary>
        public Int32 Length { get { return data.Length; } }

        /// <summary>
        /// Initializes a new instance of <see cref="Binary"/>.
        /// </summary>
        /// <param name="data">The underlying <see cref="Byte"/> array to wrap (underlying structure remains mutable).</param>
        public Binary(Byte[] data)
        {
            Verify.NotNull(data, "data");

            this.data = data;
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="other">An object to compare with this instance. </param>
        public Int32 CompareTo(Object other)
        {
            return CompareTo(other as Binary ?? other as Byte[]);
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        public Int32 CompareTo(Binary other)
        {
            return CompareTo((Byte[])other);
        }

        /// <summary>
        /// Compares the current object with another object of the same underlying type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        public Int32 CompareTo(Byte[] other)
        {
            if (other == null) return 1;

            var result = Length.CompareTo(other.Length);
            var length = Math.Min(data.Length, other.Length);
            for (var i = 0; i < length; i++)
            {
                var value = data[i].CompareTo(other[i]);
                if (value != 0)
                    return value;
            }

            return result;
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="Object"/> are equal.
        /// </summary>
        /// <param name="other">Another object to compare.</param>
        public override Boolean Equals(Object other)
        {
            return Equals(other as Binary ?? other as Byte[]);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="Binary"/> are equal.
        /// </summary>
        /// <param name="other">Another object to compare.</param>
        public Boolean Equals(Binary other)
        {
            return Equals((Byte[])other);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="T:Byte[]"/> are equal.
        /// </summary>
        /// <param name="other">Another object to compare.</param>
        public Boolean Equals(Byte[] other)
        {
            if (other == null) return false;
            if (other.Length != Length) return false;
            if (ReferenceEquals(data, other)) return true;

            for (var i = 0; i < data.Length; i++)
            {
                if (other[i] != data[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {
                var hash = 43;

                hash = (hash * 397) + GetType().GetHashCode();
                for (var i = 0; i < data.Length; i++)
                    hash = (hash * 397) + data[i].GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns a string that represents the current value object instance.
        /// </summary>
        public override String ToString()
        {
            var result = new StringBuilder();

            result.Append("0x");
            for (var i = 0; i < data.Length; i++)
                result.Append(data[i].ToString("X2"));

            return result.ToString();
        }

        /// <summary>
        /// Returns an enumerator for the underlying <see cref="T:Byte[]"/> instance.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }

        /// <summary>
        /// Returns a strongly typed enumerator for the underlying <see cref="T:Byte[]"/> instance.
        /// </summary>
        public IEnumerator<Byte> GetEnumerator()
        {
            return data.Cast<Byte>().GetEnumerator();
        }

        /// <summary>
        /// Implicitly convert a <see cref="Binary"/> instance to a <see cref="T:Byte[]"/> (i.e., unwraps <see cref="Binary"/>).
        /// </summary>
        /// <param name="value">The value object to implicitly convert.</param>
        public static implicit operator Byte[](Binary value)
        {
            return value == null ? null : value.data;
        }

        /// <summary>
        /// Implicitly convert a <see cref="T:Byte[]"/> instance to a <see cref="Binary"/> (i.e., wraps <see cref="T:Byte[]"/>).
        /// </summary>
        /// <param name="value">The value object to implicitly convert.</param>
        public static implicit operator Binary(Byte[] value)
        {
            return value == null ? null : new Binary(value);
        }
    }
}
