using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing.Mappings;
using Spark.Messaging;
using Spark.Resources;

namespace Spark.Cqrs.Eventing.Sagas
{
    //TODO: Reminder... save state and then publish commands... to handle failure, can write sagas such that a timeout is scheduled and cleared if the corresponding completion event is received...
    //      Thus state is saved, command is published and if required and if failed, can be re-published on timeout if design requires it... (i.e., saga handler doesn't need to deal with it)...
    [EventHandler(IsReusable = false)]
    public abstract class Saga : StateObject
    {
        [IgnoreDataMember]
        public Guid CorrelationId { get; internal set; }

        [IgnoreDataMember]
        public DateTime? Timeout { get; internal set; }

        [IgnoreDataMember]
        public Boolean Completed { get; internal set; }

        [IgnoreDataMember]
        internal Int32 Version { get; set; }

        [IgnoreDataMember]
        internal Guid TypeId { get; set; }

        /// <summary>
        /// Creates a deep-copy of the current saga object graph by traversing all public and non-public fields.
        /// </summary>
        /// <remarks>Aggregate object graph must be non-recursive.</remarks>
        protected internal virtual Saga Copy()
        {
            return ObjectCopier.Copy(this);
        }

        protected void Publish()
        {
            //TODO: Overloads... Publish(Command), Publish(Command, Headers)
            //TODO: Merge headers from event context but always overwrite timestamp header...

        }

        protected void Publish(Guid aggregateId, Command command)
        {
            Publish(aggregateId, command, HeaderCollection.Empty);
        }

        protected void Publish(Guid aggregateId, Command command, params Header[] headers)
        {
            Publish(aggregateId, command, (IEnumerable<Header>)headers);
        }

        protected void Publish(Guid aggregateId, Command command, IEnumerable<Header> headers)
        {
            Verify.NotEqual(Guid.Empty, aggregateId, "aggregateId");
            Verify.NotNull(command, "command");

            //TODO: Publish (use MessageFactory ... so will need to be accessible from sagacontext :9)
        }

        protected abstract void RegisterEvents(); //TODO: Remove

        //TODO: Use SagaConfiguration instance to create SagaMetadata instance (i.e., ensure immutable after intial wire-up).
        //TODO: Ensure that number of mapped events matches number of known handle methods.
        //TODO: If both StartWith and handle called, throw exception.
        //TODO: Enforce public default ctor.
        //TODO: protected abstract void Configure(SagaConfiguration saga);
        protected virtual void Configure(SagaConfiguration saga)
        { }

        protected void ScheduleTimeout(TimeSpan timeout)
        {
            ScheduleTimeout(DateTime.UtcNow.Add(timeout));
        }

        protected void ScheduleTimeout(DateTime timeout)
        {
            //TODO: Ensure universal.
        }

        protected void ClearTimeout()
        {

        }

        protected void MarkCompleted()
        {

        }

        public override string ToString()
        {
            return String.Format("{0} - {1}", GetType(), CorrelationId);
        }

        protected void Handle(Object e)
        {
            ClearTimeout(); //TODO: If timeout stored with saga, need to ensure current timeout cleared when timeout handled.

            //TODO: Call some other virtual method for actual processing... 
        }



        internal static SagaMetadata GetMetadata(Type sagaType, HandleMethodCollection handleMethods)
        {
            Verify.NotNull(sagaType, "sagaType");
            Verify.NotNull(handleMethods, "handleMethods");
            Verify.TypeDerivesFrom(typeof(Saga), sagaType, "sagaType");

            var saga = (Saga)Activator.CreateInstance(sagaType);
            var metadata = saga.GetMetadata();
            var initiatingEvents = 0;

            foreach (var handleMethod in handleMethods)
            {
                if (metadata.CanStartWith(handleMethod.Key))
                    initiatingEvents++;

                if (metadata.CanHandle(handleMethod.Key))
                    continue;

                    throw new MappingException(Exceptions.EventTypeNotConfigured.FormatWith(sagaType, handleMethod.Key));
            }
            
            if (initiatingEvents == 0)
                throw new MappingException(Exceptions.SagaMustHaveAtLeastOneInitiatingEvent.FormatWith(sagaType));

            return metadata;
        }

        private SagaMetadata GetMetadata()
        {
            var configuration = new SagaConfiguration(GetType());

            //TODO: configuration.CanHandle((Timeout e) => e.SagaId);

            Configure(configuration);

            return configuration.GetMetadata();
        }


        private static readonly IDictionary<SagaReference, SagaLock> SagaLocks = new Dictionary<SagaReference, SagaLock>();
        private static readonly Object GlobalLock = new Object();

        internal static SagaLockToken AquireLock(Type sagaType, Guid sagaId)
        {
            var reference = new SagaReference(sagaType, sagaId);
            var sagaLock = default(SagaLock);

            lock (GlobalLock)
            {
                if (!SagaLocks.TryGetValue(reference, out sagaLock))
                    SagaLocks.Add(reference, sagaLock = new SagaLock());

                sagaLock.Increment();
            }

            Monitor.Enter(sagaLock);

            return new SagaLockToken(reference, sagaLock);
        }

        internal static void ReleaseLock(SagaLockToken cookie)
        {
            var sagaLock = cookie.SagaLock;

            Monitor.Exit(sagaLock);

            lock (GlobalLock)
            {
                if (sagaLock.Decrement() == 0)
                    SagaLocks.Remove(cookie.Reference);
            }
        }

    }


}
