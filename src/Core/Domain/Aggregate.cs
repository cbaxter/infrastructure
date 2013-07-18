using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Resources;

/* Copyright (c) 2012 Spark Software Ltd.
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

namespace Spark.Infrastructure.Domain
{
    /// <summary>
    /// A collection of <see cref="Entity"/> objects that are bound together by this root entity.
    /// </summary>
    public abstract class Aggregate : Entity
    {
        [NonSerialized]
        private Int32 version;

        [NonSerialized, NonHashed]
        private Guid checksum;

        /// <summary>
        /// The aggregate revision.
        /// </summary>
        [IgnoreDataMember]
        public Int32 Version { get { return version; } internal set { version = value; } }
        
        /// <summary>
        /// Creates a deep-copy of the current aggregate object graph by traversing all public and non-public fields.
        /// </summary>
        /// <remarks>Aggregate object graph must be non-recursive.</remarks>
        protected internal virtual Aggregate Copy()
        {
            return ObjectCopier.Copy(this);
        }

        /// <summary>
        /// Update the aggregate checksum based on the currently known <see cref="Aggregate"/> state.
        /// </summary>
        /// <remarks>
        /// Fields marked with <see cref="IgnoreDataMemberAttribute"/>, <see cref="NonSerializedAttribute"/> and/or <see cref="XmlIgnoreAttribute"/> will not
        /// be included when calculating the MD5 hash of this non-recursive object graph.
        /// </remarks>
        protected internal virtual void UpdateHash()
        {
            checksum = ObjectHasher.Hash(this);
        }

        /// <summary>
        /// Validates the <see cref="Aggregate"/> state against the current checksum (state hash). If <see cref="UpdateHash"/> has not been previously called
        /// the aggregate state is assumed to be valid and the checksum is set for future reference.
        /// </summary>
        /// <remarks>
        /// Fields marked with <see cref="IgnoreDataMemberAttribute"/>, <see cref="NonSerializedAttribute"/> and/or <see cref="XmlIgnoreAttribute"/> will not
        /// be included when calculating the MD5 hash of this non-recursive object graph.
        /// </remarks>
        protected internal virtual void VerifyHash()
        {
            if (checksum == Guid.Empty)
            {
                UpdateHash();
            }
            else
            {
                if (checksum != ObjectHasher.Hash(this))
                    throw new MemberAccessException(Exceptions.StateAccessException.FormatWith(Id));
            }
        }
        
        /// <summary>
        /// Returns the <see cref="Aggregate"/> description for this instance.
        /// </summary>
        public override string ToString()
        {
            return String.Format("{0} - {1} (v{2})", GetType().FullName, Id, Version);
        }
    }
}
