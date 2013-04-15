using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.Eventing;

namespace Spark.Infrastructure.Domain
{

    //TODO: Rather than ITypeResolver just use IServiceProvider ? (Autofac Container is an IServiceProvider...)

    public struct AggregateVersion
    {
        private readonly Int32 version;
        private readonly Int32? snapshotVersion;

        public Int32 Version { get { return version; } }
        public Int32? SnapshotVersion { get { return snapshotVersion; } } //TODO: May remove and just use background service to snapshot where required ever x minutes.

        public AggregateVersion(Int32 snapshotVersion)
        {
            Verify.GreaterThan(0, snapshotVersion, "snapshotVersion");

            this.version = snapshotVersion;
            this.snapshotVersion = snapshotVersion;
        }

        public static implicit operator Int32(AggregateVersion aggregateVersion)
        {
            return aggregateVersion.Version;
        }
    }

    public abstract class Aggregate
    {
        //TODO: Confirm read-Only Properties not serialized --> http://docs.mongodb.org/ecosystem/tutorial/serialize-documents-with-the-csharp-driver/

        //TODO: Serialize Aggregate/Entity by converting all non-readonly fields in to dictionary or something... have overridable methods for Serialize and Deserialize
        //      that can be used for explicit mappings if required.

        protected readonly Object SyncLock = new Object();

        [IgnoreDataMember] //TODO: May not even be required...
        public Guid Id { get; internal set; }

        [IgnoreDataMember]
        public Int32 Version { get; internal set; }

        [IgnoreDataMember]
        public Int32 SnapshotVersion { get; internal set; } //TODO: Remove (just snapshot on load if required... )

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

    // public void Handle(DeregulateMeasurementPoint command)
    // {
    //      if(MeasurementPoints.ContainsKey(command.Name)
    //      {
    //          ....
    //      }
    //      else
    //      {
    //          // Defer the command for up to five minutes. When a command is processed, see if any deferred commands can be processed (maintaining order). Would be nice to have a 
    //          // simplier DeferCommand(command, predicate) approach, but how to confitionally abort? Could go TryExecuteCommand(predicate, handler, command) but kind of fugly... 
    //          // ideally want to maintain order of commands? this approach will dequeue and re-queue commands as processed. May want to raise explicit event on timeout?
    //          DeferCommand(command, TimeSpan.FromMinutes(5)); //TODO: Do we really need a configurable timeout? really should just be an app setting...
    //          Defer(command, anOptionaleventToRaiseOnTimeout)?
    //      }

    public class FieldAsset : Aggregate
    {
        protected virtual Int32 MyProperty { get; private set; }
        protected virtual KeyedCollection<Guid, Object> KeyedCollection { get; private set; }


        //public void Handle(Object x)
        //{
        //    Publish(null, new MessageHeader(MessageHeaders.UserId, Guid.NewGuid());
        //    Publish(null, MessageHeader.FromUserId(xyz)));
        //}

        //public void Apply(Event e)
        //{}
    }

    public class FieldAssetCommandHandler : FieldAsset //TODO: Would be nice... but also wrong... more accurate would be CommandHandler<FieldAsset> :(
    {
        //public void Handle(Command command)
        //{
        //    //Raise();
        //}
    }


    // for commands/events could assume if id or aggregatenameId then is aggregate id? or have explicit attribute?
    // or could put as `special` headers on Headers property? So command.Headers.AggregateId ? then could optimize in base command if desired by exposing as OrderId { get { return Headers.AggregateId; } } with an ignore attribute!
    // could even allow Aggegate<BaseCommand> option for type safety where default is just <Command> could go further and allow for <Command,Event> but may have more generalized events... although could allow for it...

    // infer aggregateId on command based on aggregate name + id suffix or just id... otherwise require explicit [AggregateId] attribute?
}
