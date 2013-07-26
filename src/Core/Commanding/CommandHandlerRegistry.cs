using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Spark.Domain;
using Spark.Domain.Mappings;
using Spark.Logging;
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

namespace Spark.Commanding
{
    /// <summary>
    /// A <see cref="CommandHandler"/> registry associating <see cref="Aggregate"/> handle methods with specific <see cref="Command"/> types.
    /// </summary>
    public sealed class CommandHandlerRegistry : IRetrieveCommandHandlers
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<Type, CommandHandler> knownCommandHandlers;

        /// <summary>
        /// Initializes a new instance of <see cref="CommandHandlerRegistry"/> with the specified <paramref name="typeLocator"/> and <paramref name="serviceProvider"/>.
        /// </summary>
        /// <param name="aggregateStore">The <see cref="Aggregate"/> store.</param>
        /// <param name="typeLocator">The type locator use to retrieve all known <see cref="Aggregate"/> types.</param>
        /// <param name="serviceProvider">The service locator used to retrieve singleton command handler dependencies.</param>
        public CommandHandlerRegistry(IStoreAggregates aggregateStore, ILocateTypes typeLocator, IServiceProvider serviceProvider)
        {
            Verify.NotNull(typeLocator, "typeLocator");
            Verify.NotNull(serviceProvider, "serviceProvider");

            knownCommandHandlers = DiscoverCommandHandlers(aggregateStore, typeLocator, serviceProvider);
        }

        /// <summary>
        /// Discover all command handlers associated with any locatable <see cref="Aggregate"/> type.
        /// </summary>
        /// <param name="aggregateStore">The <see cref="Aggregate"/> store.</param>
        /// <param name="typeLocator">The type locator use to retrieve all known <see cref="Aggregate"/> types.</param>
        /// <param name="serviceProvider">The service locator used to retrieve singleton command handler dependencies.</param>
        private static Dictionary<Type, CommandHandler> DiscoverCommandHandlers(IStoreAggregates aggregateStore, ILocateTypes typeLocator, IServiceProvider serviceProvider)
        {
            var knownHandleMethods = DiscoverHandleMethods(typeLocator, serviceProvider);
            var result = new Dictionary<Type, CommandHandler>();
            var logMessage = new StringBuilder();

            logMessage.Append("Discovered command handler methods:");
            foreach (var aggregateMapping in knownHandleMethods.OrderBy(kvp => kvp.Key.FullName))
            {
                logMessage.Append("    ");
                logMessage.Append(aggregateMapping.Key);
                logMessage.AppendLine();

                foreach (var handleMethodMapping in aggregateMapping.Value.OrderBy(kvp => kvp.Key.FullName))
                {
                    logMessage.Append("        ");
                    logMessage.Append(handleMethodMapping.Key);
                    logMessage.AppendLine();

                    if (result.ContainsKey(handleMethodMapping.Key))
                        throw new MappingException(Exceptions.HandleMethodMustBeAssociatedWithSingleAggregate.FormatWith(aggregateMapping.Key, handleMethodMapping.Key));

                    result.Add(handleMethodMapping.Key, new CommandHandler(aggregateMapping.Key, handleMethodMapping.Key, aggregateStore, handleMethodMapping.Value));
                }
            }

            Log.Debug(logMessage.ToString);

            return result;
        }

        /// <summary>
        /// Discover all command handlers methods associated with any locatable <see cref="Aggregate"/> type.
        /// </summary>
        /// <param name="typeLocator">The type locator use to retrieve all known <see cref="Aggregate"/> types.</param>
        /// <param name="serviceProvider">The service locator used to retrieve singleton command handler dependencies.</param>
        private static Dictionary<Type, HandleMethodCollection> DiscoverHandleMethods(ILocateTypes typeLocator, IServiceProvider serviceProvider)
        {
            var aggregateTypes = typeLocator.GetTypes(type => !type.IsAbstract && type.IsClass && type.DerivesFrom(typeof(Aggregate)));
            var knownCommandHandlers = new Dictionary<Type, HandleMethodCollection>();

            foreach (var aggregateType in aggregateTypes)
            {
                var handleMethods = GetHandleMethods(aggregateType, serviceProvider);

                knownCommandHandlers.Add(aggregateType, handleMethods);
            }

            return knownCommandHandlers;
        }

        /// <summary>
        ///  Discover all command handlers methods associated with the specified <paramref name="aggregateType"/>.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        /// <param name="serviceProvider">The service locator used to retrieve singleton command handler dependencies.</param>
        private static HandleMethodCollection GetHandleMethods(Type aggregateType, IServiceProvider serviceProvider)
        {
            var handleMethodMappings = aggregateType.GetCustomAttributes<HandleByStrategyAttribute>().ToArray();
            if (handleMethodMappings.Length > 1)
                throw new MappingException(Exceptions.AggregateHandleByStrategyAmbiguous.FormatWith(aggregateType));

            return (handleMethodMappings.Length == 0 ? HandleByStrategyAttribute.Default : handleMethodMappings[0]).GetHandleMethods(aggregateType, serviceProvider);
        }

        /// <summary>
        /// Gets the <see cref="CommandHandler"/> associated with the specified <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command for which to retrieve a <see cref="CommandHandler"/> instance.</param>
        public CommandHandler GetHandlerFor(Command command)
        {
            Verify.NotNull(command, "command");

            CommandHandler commandHandler;
            Type commandType = command.GetType();
            if (!knownCommandHandlers.TryGetValue(commandType, out commandHandler))
                throw new MappingException(Exceptions.CommandHandlerNotFound.FormatWith(commandType));

            return commandHandler;
        }
    }
}
