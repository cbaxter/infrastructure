using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Spark.Cqrs.Commanding;
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

namespace Spark.Cqrs.Domain
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
        /// The aggregate revision used to detect concurrency conflicts.
        /// </summary>
        [IgnoreDataMember]
        public Int32 Version { get { return version; } internal set { version = value; } }

        /// <summary>
        /// Returns <value>true</value> if this aggregate has already been created; otherwise returns <value>false</value>.
        /// </summary>
        [IgnoreDataMember]
        protected Boolean Created { get { return Version > 0; } }

        /// <summary>
        /// Return <value>true</value> if this aggregate must be explicitly created by another <see cref="Aggregate"/> instance or an exception 
        /// has been made via <see cref="CanCreateAggregate"/>; otherwise return <value>false</value> (default = <value>true</value>).
        /// </summary>
        [IgnoreDataMember]
        protected virtual Boolean RequiresExplicitCreate { get { return true; } }

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
        /// Validates the <see cref="Aggregate"/> state against the current checksum (state hash). If <see cref="UpdateHash"/> has not been
        /// previously called the aggregate state is assumed to be valid and the checksum is set for future reference.
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
        /// Verify that the specified command can be handled by this aggregate instance.
        /// </summary>
        /// <param name="command">The command to be handled.</param>
        internal void VerifyCanHandleCommand(Command command)
        {
            if (Version == 0 && RequiresExplicitCreate && !CanCreateAggregate(command))
                throw new InvalidOperationException(Exceptions.AggregateNotInitialized.FormatWith(GetType(), Id));
        }

        /// <summary>
        /// Return <value>true</value> if the <paramref name="command"/> can create a new aggregateinstance; otherwise return <value>false</value> (default = <value>false</value>).
        /// </summary>
        /// <param name="command">The command attempting to create this aggregate instance.</param>
        protected virtual Boolean CanCreateAggregate(Command command)
        {
            return false;
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the aggregate <see cref="Version"/> is equal to zero (i.e., the aggregate did not previously exist).
        /// </summary>
        protected void VerifyInitialized()
        {
            if (version == 0) throw new InvalidOperationException(Exceptions.AggregateNotInitialized.FormatWith(GetType(), Id));
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the aggregate <see cref="Version"/> is greater than zero (i.e., the aggregate already exists).
        /// </summary>
        protected void VerifyUninitialized()
        {
            if (version > 0) throw new InvalidOperationException(Exceptions.AggregateInitialized.FormatWith(GetType(), Id, Version));
        }

        /// <summary>
        /// Get the underlying <see cref="Aggregate"/> mutable state information.
        /// </summary>
        protected internal override IDictionary<String, Object> GetState()
        {
            var state = base.GetState();

            // NOTE: Although entities consider the ID as a part of their state; aggregates are the exception.
            //       Remove the `id` attribute from state if required to avoid duplicate representation.
            state.Remove(Property.Id);

            return state;
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
