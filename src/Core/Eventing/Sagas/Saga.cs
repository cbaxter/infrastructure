using System;
using Spark.Infrastructure.Eventing.Mappings;

namespace Spark.Infrastructure.Eventing.Sagas
{
    //TODO: Reminder... save state and then publish commands... to handle failure, can write sagas such that a timeout is scheduled and cleared if the corresponding completion event is received...
    //      Thus state is saved, command is published and if required and if failed, can be re-published on timeout if design requires it... (i.e., saga handler doesn't need to deal with it)...

    [EventHandler(IsReusable = false)]
    public abstract class Saga : StateObject
    {
        public Guid CorrelationId { get; internal set; }

        protected void Publish()
        {

        }

        protected abstract void RegisterEvents();

        protected void ScheduleTimeout(DateTime timeout)
        {

        }

        protected void ScheduleTimeout(TimeSpan timeout)
        {

        }

        protected void ClearTimeout()
        {

        }

        protected void MarkCompleted()
        {
            
        }

        protected void Handle(Object e)
        {
            ClearTimeout(); //TODO: If timeout stored right with saga, need to ensure current timeout cleared when timeout handled.

            //TODO: Call some other virtual method for actual processing... 
        }
    }

    //public class MySaga : Saga
    //{
    //    protected override void Configure(SagaConfiguration saga)
    //    {
    //        saga.CanStartWith((MyEvent e) => e.AggregateId);
    //        saga.CanHandle((MyOtherEvent e) => e.AggregateId);

    //        //TODO: Ensure that number of mapped events matches number of known handle methods.
    //        //TODO: If both StartWith and handle called, throw exception.
    //        //TODO: Enforce public default ctor.
    //    }
    //}
}
