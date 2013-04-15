using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Infrastructure.Eventing
{
    class Thoughts
    {
        // Only need single [Handle] attribute convention etc with property for "StartWith = true/false"
        // Other convenstions can scan for/handle start with as well... only difference will be that 
        // anything `StartWith` related will throw a mapping exception if not derive from Saga
        //
        // In terms of saga, will need simple repository impl (single node), fancy for multi-node (or just to saga event bus with another partitioned scheduler)?
        // EventHandlerRegistry should be extended by SagaHandlerRegistry with simple overrides for coaping with StartWith.
        // 
        // May want explicit EventMessage (and CommandMessage?) where EventMessage has AggregateType and AggregateId as custom properties
        // the base Event class can then have protected members for GetAggregateType() and GetAggregateId(). Will also want to have SagaHandlerRegistry
        // have both GetHandlersFor(Event e) and GetSagaIdFor(Event e) where the latter is used for partitioning...
    }
}
