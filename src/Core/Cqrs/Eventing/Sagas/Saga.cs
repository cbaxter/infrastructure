using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing.Mappings;
using Spark.Logging;
using Spark.Messaging;
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

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// Coordinates and routes messages between bounded contexts and aggregats.
    /// </summary>
    [Serializable, EventHandler(IsReusable = false)]
    public abstract class Saga : StateObject
    {
        private static readonly IDictionary<Type, SagaMetadata> SagaMetadataCache = new ConcurrentDictionary<Type, SagaMetadata>();
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The saga correlation identifier associated with this saga instance.
        /// </summary>
        [IgnoreDataMember]
        public Guid CorrelationId { get; internal set; }

        /// <summary>
        /// The UTC timestamp identifying when the next saga timeout will occur or <value>null</value> if no timeout scheduled.
        /// </summary>
        [IgnoreDataMember]
        public DateTime? Timeout { get; internal set; }

        /// <summary>
        /// Returns <value>true</value> if a <see cref="Timeout"/> is current scheduled; otherwise <value>false</value>.
        /// </summary>
        [IgnoreDataMember]
        public Boolean TimeoutScheduled { get { return Timeout.HasValue; } }

        /// <summary>
        /// Returns <value>true</value> if this saga  instance has completed; otherwise <value>false</value>.
        /// </summary>
        [IgnoreDataMember]
        public Boolean Completed { get; internal set; }

        /// <summary>
        /// The saga revision used to detect concurrency conflicts.
        /// </summary>
        [IgnoreDataMember]
        public Int32 Version { get; internal set; }

        /// <summary>
        /// Gets the saga metadata for this saga instance.
        /// </summary>
        /// <remarks>Called once during saga discovery.</remarks>
        internal SagaMetadata GetMetadata()
        {
            Type sagaType = GetType();
            SagaMetadata sagaMetadata;

            if (!SagaMetadataCache.TryGetValue(sagaType, out sagaMetadata))
            {
                var configuration = new SagaConfiguration(GetType());

                Configure(configuration);
                SagaMetadataCache[sagaType] = sagaMetadata = configuration.GetMetadata();
            }

            return sagaMetadata;
        }

        /// <summary>
        /// Configure the saga event handling for this saga type.
        /// </summary>
        /// <param name="saga">The saga configuration instance used to collect saga metadata.</param>
        protected abstract void Configure(SagaConfiguration saga);

        /// <summary>
        /// Creates a deep-copy of the current saga object graph by traversing all public and non-public fields.
        /// </summary>
        /// <remarks>Aggregate object graph must be non-recursive.</remarks>
        protected internal virtual Saga Copy()
        {
            return ObjectCopier.Copy(this);
        }

        /// <summary>
        /// Mark this saga instance as completed.
        /// </summary>
        protected void MarkCompleted()
        {
            Completed = true;
            Log.Trace("Saga completed");
        }

        /// <summary>
        /// Clear an existing scheduled timeout.
        /// </summary>
        protected internal void ClearTimeout()
        {
            if (!TimeoutScheduled)
                throw new InvalidOperationException(Exceptions.SagaTimeoutNotScheduled.FormatWith(GetType(), CorrelationId));

            Timeout = null;
            FlagTimeoutChangedOnSagaContext();
            Log.Trace("Timeout cleared");
        }

        /// <summary>
        /// Scheduled a new <paramref name="timeout"/>.
        /// </summary>
        /// <param name="timeout">The time from now when a timeout should occur.</param>
        protected void ScheduleTimeout(TimeSpan timeout)
        {
            ScheduleTimeout(SystemTime.Now.Add(timeout));
        }

        /// <summary>
        /// Scheduled a new <paramref name="timeout"/>.
        /// </summary>
        /// <param name="timeout">The date/time when a timeout should occur.</param>
        protected void ScheduleTimeout(DateTime timeout)
        {
            if (!GetMetadata().CanHandle(typeof(Timeout)))
                throw new InvalidOperationException(Exceptions.SagaTimeoutNotHandled.FormatWith(GetType()));

            if (TimeoutScheduled)
                throw new InvalidOperationException(Exceptions.SagaTimeoutAlreadyScheduled.FormatWith(GetType(), CorrelationId));

            if (timeout.Kind != DateTimeKind.Utc)
                timeout = timeout.ToUniversalTime();

            Timeout = timeout;
            FlagTimeoutChangedOnSagaContext();
            Log.Trace("Timeout scheduled for {0}", timeout);
        }

        /// <summary>
        /// Clear existing timeout if scheduled and schedule a new <paramref name="timeout"/>.
        /// </summary>
        /// <param name="timeout">The time from now when a timeout should occur.</param>
        protected void RescheduleTimeout(TimeSpan timeout)
        {
            RescheduleTimeout(SystemTime.Now.Add(timeout));
        }

        /// <summary>
        /// Clear existing timeout if scheduled and schedule a new <paramref name="timeout"/>.
        /// </summary>
        /// <param name="timeout">The date/time when a timeout should occur.</param>
        protected void RescheduleTimeout(DateTime timeout)
        {
            ClearTimeout();
            ScheduleTimeout(timeout);
        }

        /// <summary>
        /// Update the saga context to reflect that the saga timeout has changed.
        /// </summary>
        private static void FlagTimeoutChangedOnSagaContext()
        {
            var context = SagaContext.Current;
            if (context == null)
                throw new InvalidOperationException(Exceptions.NoSagaContext);

            context.TimeoutChanged = true;
        }

        /// <summary>
        /// Publishes the specified <paramref name="command"/> with only the default message headers.
        /// </summary>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="command"/>.</param>
        /// <param name="command">The command to be published.</param>
        protected void Publish(Guid aggregateId, Command command)
        {
            Publish(aggregateId, command, (IEnumerable<Header>)null);
        }

        /// <summary>
        /// Publishes the specified <paramref name="command"/> with the set of custom message headers.
        /// </summary>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="command"/>.</param>
        /// <param name="command">The command to be published.</param>
        /// <param name="headers">The set of one or more custom message headers.</param>
        protected void Publish(Guid aggregateId, Command command, params Header[] headers)
        {
            Publish(aggregateId, command, headers == null || headers.Length == 0 ? (IEnumerable<Header>)null : headers);
        }

        /// <summary>
        /// Publishes the specified <paramref name="command"/> with the enumerable set of custom message headers.
        /// </summary>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="command"/>.</param>
        /// <param name="command">The command to be published.</param>
        /// <param name="headers">The set of message headers associated with the command.</param>
        protected void Publish(Guid aggregateId, Command command, IEnumerable<Header> headers)
        {
            Log.Trace("Publishing {0} command to aggregate {1}", command, aggregateId);

            var context = SagaContext.Current;
            if (context == null)
                throw new InvalidOperationException(Exceptions.NoSagaContext);

            context.Publish(aggregateId, headers == null ? GetHeadersFromEventContext() : headers.Concat(GetHeadersFromEventContext()).Distinct(header => header.Name), command);

            Log.Trace("Published {0} command to aggregate {1}", command, aggregateId);
        }

        /// <summary>
        /// Get the core headers from the current <see cref="EventContext"/>.
        /// </summary>
        private static IEnumerable<Header> GetHeadersFromEventContext()
        {
            var context = EventContext.Current;
            if (context == null)
                throw new InvalidOperationException(Exceptions.NoEventContext);
            
            var value = String.Empty;
            var result = new List<Header>();
            var eventHeaders = context.Headers;

            if (eventHeaders.TryGetValue(Header.Origin, out value))
                result.Add(new Header(Header.Origin, value, checkReservedNames: false));

            if (eventHeaders.TryGetValue(Header.RemoteAddress, out value))
                result.Add(new Header(Header.RemoteAddress, value, checkReservedNames: false));

            if (eventHeaders.TryGetValue(Header.UserAddress, out value))
                result.Add(new Header(Header.UserAddress, value, checkReservedNames: false));

            if (eventHeaders.TryGetValue(Header.UserName, out value))
                result.Add(new Header(Header.UserName, value, checkReservedNames: false));

            return result;
        }

        /// <summary>
        /// Returns the saga description for this instance.
        /// </summary>
        public override String ToString()
        {
            return String.Concat(GetType(), " - ", CorrelationId);
        }
    }
}
