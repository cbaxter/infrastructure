using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Cqrs.Eventing.Sagas
{
    public sealed class SagaConfiguration
    {
        public void CanStartWith<TEvent>(Func<TEvent, Guid> resolver)
        {

        }

        public void CanHandle<TEvent>(Func<TEvent, Guid> resolver)
        {

        }
    }
}
