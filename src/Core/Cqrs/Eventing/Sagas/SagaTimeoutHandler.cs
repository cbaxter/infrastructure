using System;
using Spark.Configuration;
using Spark.Cqrs.Commanding;
using Spark.Logging;

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

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// Represents an <see cref="Event"/> handler method executor associated with a given <see cref="Saga"/>.
    /// </summary>
    public sealed class SagaTimeoutHandler : SagaEventHandler
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of <see cref="SagaEventHandler"/>.
        /// </summary>
        /// <param name="eventHandler">The base event handler to decorate.</param>
        /// <param name="sagaMetadata">The saga metadata associated with this saga event handler.</param>
        /// <param name="sagaStore">The saga store used to load/save saga state.</param>
        /// <param name="commandPublisher">The command publisher used to publish saga commands.</param>
        internal SagaTimeoutHandler(EventHandler eventHandler, SagaMetadata sagaMetadata, IStoreSagas sagaStore, Lazy<IPublishCommands> commandPublisher)
            : base(eventHandler, sagaMetadata, sagaStore, commandPublisher, Settings.SagaStore)
        { }

        /// <summary>
        /// Handles the <paramref name="saga"/> event.
        /// </summary>
        /// <param name="saga">The saga instance handling the event.</param>
        /// <param name="e">The event to be handled.</param>
        protected override void HandleSagaEvent(Saga saga, Event e)
        {
            var timeout = ((Timeout)e).Scheduled;

            if (saga.Timeout.HasValue)
            {
                if (saga.Timeout.Value == timeout)
                {
                    saga.ClearTimeout();
                    base.HandleSagaEvent(saga, e);
                }
                else
                {
                    Log.Warn("{0} received unexpected timeout at {1} when scheduled timeout is for {2}", saga, timeout.ToString(DateTimeFormat.RoundTrip), saga.Timeout.Value.ToString(DateTimeFormat.RoundTrip));
                }
            }
            else
            {
                Log.Warn("{0} received unexpected timeout at {1} when no timeout is scheduled", saga, timeout.ToString(DateTimeFormat.RoundTrip));
            }
        }
    }
}
