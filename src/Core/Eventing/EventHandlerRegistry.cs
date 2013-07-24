using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Spark.Infrastructure.Domain;
using Spark.Infrastructure.Eventing.Mappings;
using Spark.Infrastructure.Eventing.Sagas;
using Spark.Infrastructure.Logging;
using Spark.Infrastructure.Resources;

/* Copyright (c) 2012 Spark Software Ltd.
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

namespace Spark.Infrastructure.Eventing
{
    /// <summary>
    /// An <see cref="EventHandler"/> registry associating event handler handle methods with specific <see cref="Event"/> types.
    /// </summary>
    public sealed class EventHandlerRegistry : IRetrieveEventHandlers
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IDictionary<Type, EventHandler[]> knownEventHandlers;

        /// <summary>
        /// Initializes a new instance of <see cref="EventHandlerRegistry"/> with the specified <paramref name="typeLocator"/> and <paramref name="serviceProvider"/>.
        /// </summary>
        /// <param name="typeLocator">The type locator use to retrieve all known <see cref="Event"/> types.</param>
        /// <param name="serviceProvider">The service locator used to retrieve singleton event handler dependencies.</param>
        public EventHandlerRegistry(ILocateTypes typeLocator, IServiceProvider serviceProvider)
        {
            Verify.NotNull(typeLocator, "typeLocator");
            Verify.NotNull(serviceProvider, "serviceProvider");

            knownEventHandlers = DiscoverEventHandlers(typeLocator, serviceProvider);
        }

        /// <summary>
        /// Discover all event handlers associated with any locatable class marked with <see cref="EventHandlerAttribute"/>.
        /// </summary>
        /// <param name="typeLocator">The type locator use to retrieve all known classes marked with <see cref="EventHandlerAttribute"/>.</param>
        /// <param name="serviceProvider">The service locator used to retrieve singleton event handler dependencies.</param>
        private static Dictionary<Type, EventHandler[]> DiscoverEventHandlers(ILocateTypes typeLocator, IServiceProvider serviceProvider)
        {
            var knownEvents = typeLocator.GetTypes(type => !type.IsAbstract && type.IsClass && type.DerivesFrom(typeof(Event)));
            var knownHandlers = DiscoverHandleMethods(typeLocator, serviceProvider);
            var result = new Dictionary<Type, EventHandler[]>();
            var logMessage = new StringBuilder();

            foreach (var eventType in knownEvents.OrderBy(type => type.FullName))
            {
                var eventHandlers = eventType.GetTypeHierarchy().Reverse().Where(knownHandlers.ContainsKey).SelectMany(type => knownHandlers[type]).OrderBy(handler => handler is SagaEventHandler).ToArray();

                logMessage.Append("    ");
                logMessage.Append(eventType);
                logMessage.AppendLine();

                foreach (var eventHandler in eventHandlers)
                {
                    logMessage.Append("        ");
                    logMessage.Append(eventHandler.HandlerType);
                    logMessage.AppendLine();
                }

                result.Add(eventType, eventHandlers);
            }

            Log.Debug(logMessage.ToString);

            return result;
        }

        /// <summary>
        /// Discover all event handlers methods associated with any locatable class marked with with <see cref="EventHandlerAttribute"/>.
        /// </summary>
        /// <param name="typeLocator">The type locator use to retrieve all known classes marked with <see cref="EventHandlerAttribute"/>.</param>
        /// <param name="serviceProvider">The service locator used to retrieve singleton event handler dependencies.</param>
        private static Dictionary<Type, List<EventHandler>> DiscoverHandleMethods(ILocateTypes typeLocator, IServiceProvider serviceProvider)
        {
            var handlerTypes = typeLocator.GetTypes(type => !type.IsAbstract && type.IsClass && type.GetCustomAttribute<EventHandlerAttribute>() != null);
            var knownEventHandlers = new Dictionary<Type, List<EventHandler>>();

            foreach (var handlerType in handlerTypes)
            {
                var handleMethods = GetHandleMethods(handlerType, serviceProvider);

                foreach (var handleMethod in handleMethods)
                {
                    List<EventHandler> eventHandlers;
                    if (!knownEventHandlers.TryGetValue(handleMethod.Key, out eventHandlers))
                        knownEventHandlers.Add(handleMethod.Key, eventHandlers = new List<EventHandler>());

                    var eventHandler = new EventHandler(handlerType, handleMethod.Key, handleMethod.Value, GetHandlerFactory(handlerType, serviceProvider));
                    eventHandlers.Add(typeof(Saga).IsAssignableFrom(handlerType) ? new SagaEventHandler(eventHandler) : eventHandler);
                }
            }

            return knownEventHandlers;
        }

        /// <summary>
        /// Gets the factory method associated with the specified <paramref name="handlerType"/>.
        /// </summary>
        /// <param name="handlerType">The event handler type.</param>
        /// <param name="serviceProvider">The service locator used to retrieve singleton event handler dependencies.</param>
        private static Func<Object> GetHandlerFactory(Type handlerType, IServiceProvider serviceProvider)
        {
            if (typeof(Saga).IsAssignableFrom(handlerType))
                return () => { throw new NotSupportedException(); };

            if (!handlerType.GetCustomAttribute<EventHandlerAttribute>().IsReusable)
                return () => serviceProvider.GetService(handlerType);

            var handler = serviceProvider.GetService(handlerType);
            return () => handler;
        }

        /// <summary>
        /// Discover all event handlers methods defined within the specified <paramref name="handlerType"/>.
        /// </summary>
        /// <param name="handlerType">The event handler type.</param>
        /// <param name="serviceProvider">The service locator used to retrieve singleton event handler dependencies.</param>
        private static HandleMethodCollection GetHandleMethods(Type handlerType, IServiceProvider serviceProvider)
        {
            if (typeof(Saga).IsAssignableFrom(handlerType) && handlerType.GetConstructor(Type.EmptyTypes) == null)
                throw new MappingException(Exceptions.SagaDefaultConstructorRequired.FormatWith(handlerType));

            var handleMethodMappings = handlerType.GetCustomAttributes<HandleByStrategyAttribute>().ToArray();
            if (handleMethodMappings.Length > 1)
                throw new MappingException(Exceptions.EventHandlerHandleByStrategyAmbiguous.FormatWith(handlerType));

            return (handleMethodMappings.Length == 0 ? HandleByStrategyAttribute.Default : handleMethodMappings[0]).GetHandleMethods(handlerType, serviceProvider);
        }

        /// <summary>
        /// Gets the set of <see cref="EventHandler"/> instances associated with the specified <paramref name="e"/>.
        /// </summary>
        /// <param name="e">The event for which to retrieve all <see cref="EventHandler"/> instances.</param>
        public IEnumerable<EventHandler> GetHandlersFor(Event e)
        {
            Verify.NotNull(e, "e");

            EventHandler[] eventHandlers;
            if (!knownEventHandlers.TryGetValue(e.GetType(), out eventHandlers))
                return Enumerable.Empty<EventHandler>();

            //TODO: If event is SagaTimeout (derive from Timeout to hide naming?)... grab handlers for type, but filter by saga type such that only one handler is returned.
            //      May even go so far as to have a secondary map of saga to timeout to allow it to be O(1) lookup for sagatimeout to saga map.

            return eventHandlers;
        }
    }
}
