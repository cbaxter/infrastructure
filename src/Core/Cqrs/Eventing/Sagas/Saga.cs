using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing.Mappings;
using Spark.Logging;
using Spark.Messaging;
using Spark.Resources;

/* Copyright (c) 2013 Spark Software Ltd.
 * 
 * This source is subject to the GNU Lesser General Public License.
 * See: http://www.gnu.org/copyleft/lesser.html
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE. 
 */

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// Coordinates and routes messages between bounded contexts and aggregats.
    /// </summary>
    [Serializable, EventHandler(IsReusable = false)]
    public abstract class Saga : StateObject
    {
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
        /// Returns <value>true</value> if this saga  instance has completed; otherwise <value>false</value>.
        /// </summary>
        [IgnoreDataMember]
        public Boolean Completed { get; internal set; }

        /// <summary>
        /// The saga version used to detect multi-node concurrency conflicts.
        /// </summary>
        [IgnoreDataMember]
        public Int32 Version { get; internal set; }

        /// <summary>
        /// The saga type identifier (MD5 hash of saga <see cref="Type.FullName"/>).
        /// </summary>
        [IgnoreDataMember]
        internal Guid TypeId { get; set; }

        /// <summary>
        /// Gets the saga metadata for the specified <paramref name="sagaType"/> validating against the known <paramref name="handleMethods"/>.
        /// </summary>
        /// <remarks>Called once during saga discovery.</remarks>
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

        /// <summary>
        /// Gets the saga metadata for this saga instance.
        /// </summary>
        /// <remarks>Called once during saga discovery.</remarks>
        private SagaMetadata GetMetadata()
        {
            var configuration = new SagaConfiguration(GetType());

            configuration.CanHandle((Timeout e) => e.SagaId);

            Configure(configuration);

            return configuration.GetMetadata();
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
        protected void ClearTimeout()
        {
            if (!Timeout.HasValue)
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
            if (Timeout.HasValue)
                throw new InvalidOperationException(Exceptions.SagaTimeoutAlreadyScheduled.FormatWith(GetType(), CorrelationId));

            if (timeout.Kind != DateTimeKind.Utc)
                timeout = timeout.ToUniversalTime();
            
            Timeout = timeout;
            FlagTimeoutChangedOnSagaContext();
            Log.TraceFormat("Timeout scheduled for {0}", timeout);
        }

        /// <summary>
        /// Clear existing timeout if scheduled and schedule a new <paramref name="timeout"/>.
        /// </summary>
        /// <param name="timeout">The time from now when a timeout should occur.</param>
        protected void RescheduleTimeout(TimeSpan timeout)
        {
            RescheduleTimeout(DateTime.UtcNow.Add(timeout));
        }

        /// <summary>
        /// Clear existing timeout if scheduled and schedule a new <paramref name="timeout"/>.
        /// </summary>
        /// <param name="timeout">The date/time when a timeout should occur.</param>
        protected void RescheduleTimeout(DateTime timeout)
        {
            if (Timeout.HasValue)
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
            Log.TraceFormat("Publishing {0} command to aggregate {1}", command, aggregateId);

            var context = SagaContext.Current;
            if (context == null)
                throw new InvalidOperationException(Exceptions.NoSagaContext);

            context.Publish(aggregateId, GetUserHeadersFromEventContext(), command);

            Log.TraceFormat("Published {0} command to aggregate {1}", command, aggregateId);
        }

        /// <summary>
        /// Publishes the specified <paramref name="command"/> with the set of custom message headers.
        /// </summary>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="command"/>.</param>
        /// <param name="command">The command to be published.</param>
        /// <param name="headers">The set of one or more custom message headers.</param>
        protected void Publish(Guid aggregateId, Command command, params Header[] headers)
        {
            Log.TraceFormat("Publishing {0} command to aggregate {1}", command, aggregateId);

            var context = SagaContext.Current;
            if (context == null)
                throw new InvalidOperationException(Exceptions.NoSagaContext);

            var baseHeaders = GetUserHeadersFromEventContext();
            context.Publish(aggregateId, headers != null && headers.Length > 0 ? headers.Concat(baseHeaders).Distinct(header => header.Name) : baseHeaders, command);

            Log.TraceFormat("Published {0} command to aggregate {1}", command, aggregateId);
        }

        /// <summary>
        /// Publishes the specified <paramref name="command"/> with the enumerable set of custom message headers.
        /// </summary>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="command"/>.</param>
        /// <param name="command">The command to be published.</param>
        /// <param name="headers">The set of message headers associated with the command.</param>
        protected void Publish(Guid aggregateId, Command command, IEnumerable<Header> headers)
        {
            Log.TraceFormat("Publishing {0} command to aggregate {1}", command, aggregateId);

            var context = SagaContext.Current;
            if (context == null)
                throw new InvalidOperationException(Exceptions.NoSagaContext);

            context.Publish(aggregateId, (headers ?? Enumerable.Empty<Header>()).Concat(GetUserHeadersFromEventContext()).Distinct(header => header.Name), command);

            Log.TraceFormat("Published {0} command to aggregate {1}", command, aggregateId);
        }

        /// <summary>
        /// Get the <value>Header.UserAddress</value> and <value>Header.UserName</value> headers from the underlying event context.
        /// </summary>
        private static IEnumerable<Header> GetUserHeadersFromEventContext()
        {
            var context = EventContext.Current;
            if (context == null)
                throw new InvalidOperationException(Exceptions.NoEventContext);

            var value = String.Empty;
            var result = new List<Header>();
            var eventHeaders = context.Headers;

            if (eventHeaders.TryGetValue(Header.UserAddress, out value) || eventHeaders.TryGetValue(Header.RemoteAddress, out value))
                result.Add(new Header(Header.UserAddress, value, checkReservedNames: false));

            if (eventHeaders.TryGetValue(Header.UserName, out value))
                result.Add(new Header(Header.UserName, value, checkReservedNames: false));

            return result;
        }

        /// <summary>
        /// Handle saga timeout.
        /// </summary>
        /// <param name="e">The saga timeout event.</param>
        [HandleMethod]
        public void Handle(Timeout e)
        {
            if (Timeout.HasValue && Timeout.Value == e.Scheduled)
            {
                ClearTimeout();
                OnTimeout(e);
            }
            else
            {
                if (Timeout.HasValue)
                    Log.WarnFormat("Unexpected timeout received for {0} when scheduled timeout is for {1}", e, Timeout.Value);
                else
                    Log.WarnFormat("Unexpected timeout received for {0} when no timeout is scheduled", e);
            }
        }

        protected virtual void OnTimeout(Timeout e)
        { }

        /// <summary>
        /// Returns the saga description for this instance.
        /// </summary>
        public override string ToString()
        {
            return String.Format("{0} - {1}", GetType(), CorrelationId);
        }
    }
}
