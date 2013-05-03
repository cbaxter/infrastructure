using System;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Eventing;

namespace Spark.Infrastructure.Domain
{
    public abstract class Aggregate
    {
        //TODO: Confirm read-Only Properties not serialized --> http://docs.mongodb.org/ecosystem/tutorial/serialize-documents-with-the-csharp-driver/

        //TODO: Serialize Aggregate/Entity by converting all non-readonly fields in to dictionary or something... have overridable methods for Serialize and Deserialize
        //      that can be used for explicit mappings if required.

        private Guid checkSum;

        public Guid Id { get; internal set; } //TODO: Ensure not serialized in snapshot

        public Int32 Version { get; internal set; } //TODO: Ensure not serialized in snapshot
        
        protected internal virtual Aggregate Copy()
        {
            return ObjectCopier.Copy(this);
        }

        protected internal virtual void VerifyCheckSum()
        {
            if (checkSum == Guid.Empty)
            {
                UpdateCheckSum();
            }
            else
            {
                if (checkSum != ObjectHasher.Hash(this))
                    throw new MemberAccessException(); //TODO: Set message
            }
        }

        protected internal virtual void UpdateCheckSum()
        {
            checkSum = ObjectHasher.Hash(this);
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
