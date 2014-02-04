using System;
using Spark.Configuration;
using Spark.Cqrs.Commanding;
using Spark.Logging;

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
    /// Represents an <see cref="Event"/> handler method executor associated with a given <see cref="Saga"/>.
    /// </summary>
    public sealed class SagaEventHandler : EventHandler
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly Lazy<IPublishCommands> lazyCommandPublisher;
        private readonly SagaMetadata sagaMetadata;
        private readonly IStoreSagas sagaStore;

        /// <summary>
        /// Initializes a new instance of <see cref="SagaEventHandler"/>.
        /// </summary>
        /// <param name="eventHandler">The base event handler to decorate.</param>
        /// <param name="sagaMetadata">The saga metadata associated with this saga event handler.</param>
        /// <param name="sagaStore">The saga store used to load/save saga state.</param>
        /// <param name="commandPublisher">The command publisher used to publish saga commands.</param>
        public SagaEventHandler(EventHandler eventHandler, SagaMetadata sagaMetadata, IStoreSagas sagaStore, Lazy<IPublishCommands> commandPublisher)
            : this(eventHandler, sagaMetadata, sagaStore, commandPublisher, Settings.SagaStore)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="SagaEventHandler"/>.
        /// </summary>
        /// <param name="eventHandler">The base event handler to decorate.</param>
        /// <param name="sagaMetadata">The saga metadata associated with this saga event handler.</param>
        /// <param name="sagaStore">The saga store used to load/save saga state.</param>
        /// <param name="commandPublisher">The command publisher used to publish saga commands.</param>
        /// <param name="settings">The event processor settings.</param>
        internal SagaEventHandler(EventHandler eventHandler, SagaMetadata sagaMetadata, IStoreSagas sagaStore, Lazy<IPublishCommands> commandPublisher, IStoreSagaSettings settings)
            : base(eventHandler)
        {
            Verify.NotNull(sagaStore, "sagaStore");
            Verify.NotNull(sagaMetadata, "sagaMetadata");
            Verify.NotNull(commandPublisher, "commandPublisher");
            Verify.NotNull(settings, "settings");

            this.sagaStore = sagaStore;
            this.sagaMetadata = sagaMetadata;
            this.lazyCommandPublisher = commandPublisher;
        }

        /// <summary>
        /// Invokes the underlying <see cref="Saga"/> event handler method using the specified <see cref="context"/>.
        /// </summary>
        /// <param name="context">The current event context.</param>
        public override void Handle(EventContext context)
        {
            var sagaId = sagaMetadata.GetCorrelationId(context.Event);

            using (var sagaContext = new SagaContext(HandlerType, sagaId, context.Event))
            {
                UpdateSaga(sagaContext);

                var commandPublisher = lazyCommandPublisher.Value;
                foreach (var sagaCommand in sagaContext.GetPublishedCommands())
                    commandPublisher.Publish(sagaCommand.AggregateId, sagaCommand.Command, sagaCommand.Headers);
            }
        }

        /// <summary>
        /// Invokes the underlying <see cref="Saga"/> event handler method using the current saga <see cref="context"/>.
        /// </summary>
        /// <param name="context">The current saga context.</param>
        private void UpdateSaga(SagaContext context)
        {
            var e = context.Event;
            var sagaId = context.SagaId;
            var sagaType = context.SagaType;
            var saga = GetOrCreateSaga(sagaType, sagaId, e);

            if (saga != null)
            {
                Log.TraceFormat("Handling event {0} on saga {1}-{2}", context.Event, sagaType, sagaId);

                Executor(saga, e);

                Log.Trace("Saving saga state");

                sagaStore.Save(saga, context);

                Log.Trace("Saga state saved");
            }
            else
            {
                Log.TraceFormat("Saga {0} is not initiated by event {1}", sagaType, context.Event);
            }
        }

        /// <summary>
        /// Get or create the target saga instance.
        /// </summary>
        /// <param name="sagaType">The saga type.</param>
        /// <param name="sagaId">The saga correlation id.</param>
        /// <param name="e">The event instance to handle.</param>
        private Saga GetOrCreateSaga(Type sagaType, Guid sagaId, Event e)
        {
            Saga saga;

            if (!sagaStore.TryGetSaga(sagaType, sagaId, out saga) && sagaMetadata.CanStartWith(e.GetType()))
                saga = sagaStore.CreateSaga(sagaType, sagaId);

            return saga;
        }
    }
}
