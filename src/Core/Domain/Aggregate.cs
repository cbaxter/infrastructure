using System;
using System.Runtime.Serialization;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Eventing;

namespace Spark.Infrastructure.Domain
{
    public abstract class Aggregate
    {
        //TODO: Confirm read-Only Properties not serialized --> http://docs.mongodb.org/ecosystem/tutorial/serialize-documents-with-the-csharp-driver/

        //TODO: Serialize Aggregate/Entity by converting all non-readonly fields in to dictionary or something... have overridable methods for Serialize and Deserialize
        //      that can be used for explicit mappings if required.

        protected readonly Object SyncLock = new Object();

        [IgnoreDataMember]
        public Guid Id { get; internal set; }

        [IgnoreDataMember]
        public Int32 Version { get; internal set; }

        protected internal virtual Aggregate Copy()
        {
            lock (SyncLock)
            {
                return ObjectCopier.Copy(this);
            }
        }

        protected void Notify(Event e)
        {
            var context = CommandContext.Current;
            if (context == null)
                throw null;

            context.Raise(e);
        }

        // could use expressions to get/set all non-readonly fields of aggregate... can be wrapped in overridable method where explicit mapping required (medium trust).
        // will likely need for Entity as well... or somehow magic map... 

        public override string ToString()
        {
            return String.Format("{0} - {1} ({2})", GetType().FullName, Id, Version);
        }
    }
}
