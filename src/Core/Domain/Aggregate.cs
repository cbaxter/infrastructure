using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Eventing;
using Spark.Infrastructure.Resources;

namespace Spark.Infrastructure.Domain
{
    public abstract class Aggregate
    {
        //TODO: Confirm read-Only Properties not serialized --> http://docs.mongodb.org/ecosystem/tutorial/serialize-documents-with-the-csharp-driver/

        //TODO: Serialize Aggregate/Entity by converting all non-readonly fields in to dictionary or something... have overridable methods for Serialize and Deserialize
        //      that can be used for explicit mappings if required.
        [NonHashed]
        private Guid checksum;

        public Guid Id { get; internal set; } //TODO: Ensure not serialized in snapshot

        public Int32 Version { get; internal set; } //TODO: Ensure not serialized in snapshot
        
        
        protected void Notify(Event e)
        {
            var context = CommandContext.Current;
            if (context == null)
                throw null;

            context.Raise(e);
        }


        // could use expressions to get/set all non-readonly fields of aggregate... can be wrapped in overridable method where explicit mapping required (medium trust).
        // will likely need for Entity as well... or somehow magic map... 

        // Start Stable Code (Aggregate not finished).
        //--------------------------------------------

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
