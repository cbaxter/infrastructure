using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Spark.Domain.Mappings;
using Spark.Eventing;
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

namespace Spark.Domain
{
    /// <summary>
    /// The <see cref="AggregateUpdater"/> is a registry associating <see cref="Aggregate"/> apply methods with specific <see cref="Event"/> types.
    /// </summary>
    public sealed class AggregateUpdater : IApplyEvents
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private static readonly Action<Aggregate, Event> VoidApplyMethod = (aggregate, e) => { };
        private readonly ReadOnlyDictionary<Type, ApplyMethodCollection> knownApplyMethods;

        /// <summary>
        /// Initializes a new instance of <see cref="AggregateUpdater"/>.
        /// </summary>
        /// <param name="typeLocator"></param>
        public AggregateUpdater(ILocateTypes typeLocator)
        {
            Verify.NotNull(typeLocator, "typeLocator");

            knownApplyMethods = new ReadOnlyDictionary<Type, ApplyMethodCollection>(DiscoverAggregates(typeLocator));
        }

        /// <summary>
        /// Discover all known <see cref="Aggregate"/> implementations known to the specified <paramref name="typeLocator"/>.
        /// </summary>
        /// <param name="typeLocator">The type locator used to retrieve all <see cref="Aggregate"/> type information.</param>
        private static IDictionary<Type, ApplyMethodCollection> DiscoverAggregates(ILocateTypes typeLocator)
        {
            var aggregateTypes = typeLocator.GetTypes(type => !type.IsAbstract && type.IsClass && type.DerivesFrom(typeof(Aggregate)));
            var result = new Dictionary<Type, ApplyMethodCollection>();

            foreach (var aggregateType in aggregateTypes)
                result.Add(aggregateType, DiscoverApplyMethods(aggregateType));

            return result;
        }

        /// <summary>
        /// Discover the set of apply methods used to update the state of the given <paramref name="aggregateType"/>.
        /// </summary>
        /// <param name="aggregateType">The aggregate type for which all apply methods are to be discovered.</param>
        private static ApplyMethodCollection DiscoverApplyMethods(Type aggregateType)
        {
            if (aggregateType.GetConstructor(Type.EmptyTypes) == null)
                throw new MappingException(Exceptions.AggregateDefaultConstructorRequired.FormatWith(aggregateType));
            
            var applyMethodMappings = aggregateType.GetCustomAttributes<ApplyByStrategyAttribute>().ToArray();
            if (applyMethodMappings.Length > 1)
                throw new MappingException(Exceptions.AggregateAmbiguousApplyMethodStrategy.FormatWith(aggregateType));

            return (applyMethodMappings.Length == 0 ? ApplyByStrategyAttribute.Default : applyMethodMappings[0]).GetApplyMethods(aggregateType);
        }

        /// <summary>
        /// Apply the specified <see cref="Event"/> <paramref name="e"/> to the provided <paramref name="aggregate"/>.
        /// </summary>
        /// <param name="e">The event to be applied to the provided <paramref name="aggregate"/>.</param>
        /// <param name="aggregate">The <see cref="Aggregate"/> instance on which the event is to be applied.</param>
        public void Apply(Event e, Aggregate aggregate)
        {
            Verify.NotNull(e, "e");
            Verify.NotNull(aggregate, "aggregate");

            var applyMethods = GetKnownApplyMethods(aggregate);
            var applyMethod = GetApplyMethod(aggregate, e, applyMethods);

            Log.DebugFormat("Applying event {0} to aggregate {1}", e, aggregate);
            
            applyMethod(aggregate, e);
        }

        /// <summary>
        /// Gets the set of known apply methods for the given <paramref name="aggregate"/> instance.
        /// </summary>
        /// <param name="aggregate">The <see cref="Aggregate"/> instance on which the event is to be applied.</param>
        private ApplyMethodCollection GetKnownApplyMethods(Aggregate aggregate)
        {
            Type aggregateType = aggregate.GetType();
            ApplyMethodCollection applyMethods;
            
            if (!knownApplyMethods.TryGetValue(aggregateType, out applyMethods))
                throw new MappingException(Exceptions.AggregateTypeUndiscovered.FormatWith(aggregate.GetType()));

            return applyMethods;
        }

        /// <summary>
        /// Get the associated apply method for the specified <see cref="Event"/> <paramref name="e"/> from the known <paramref name="applyMethods"/>.
        /// </summary>
        /// <param name="aggregate">The <see cref="Aggregate"/> instance on which the event is to be applied.</param>
        /// <param name="e">The <see cref="Event"/> to be applied.</param>
        /// <param name="applyMethods">The set of known apply methods for a given aggregate instance</param>
        private static Action<Aggregate, Event> GetApplyMethod(Aggregate aggregate, Event e, ApplyMethodCollection applyMethods)
        {
            Action<Aggregate, Event> applyMethod;
            Type eventType = e.GetType();

            if (applyMethods.TryGetValue(eventType, out applyMethod))
                return applyMethod;
            
            if (!applyMethods.ApplyOptional)
                throw new MappingException(Exceptions.AggregateApplyMethodNotFound.FormatWith(aggregate.GetType(), e.GetType()));

            return VoidApplyMethod;
        }
    }
}