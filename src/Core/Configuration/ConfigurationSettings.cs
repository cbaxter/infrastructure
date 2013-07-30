using System.Configuration;
using Spark.Cqrs.Eventing.Sagas;
using Spark.EventStore;

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

namespace Spark.Configuration
{
    /// <summary>
    /// The main configuration section where all custom settings are contained.
    /// </summary>
    internal interface ISettings
    {
        /// <summary>
        /// The <see cref="AggregateStore"/> configuration settings.
        /// </summary>
        IStoreAggregateSettings AggregateStore { get; }

        /// <summary>
        /// The <see cref="CommandProcessor"/> configuration settings.
        /// </summary>
        IProcessCommandSettings CommandProcessor { get; }

        /// <summary>
        /// The <see cref="IStoreEvents"/> configuration settings.
        /// </summary>
        IStoreEventSettings EventStore { get; }

        /// <summary>
        /// The <see cref="EventProcessor"/> configuration settings.
        /// </summary>
        IProcessEventSettings EventProcessor { get; }

        /// <summary>
        /// The <see cref="IStoreSagas"/> configuration settings.
        /// </summary>
        IStoreSagaSettings SagaStore { get; }

        /// <summary>
        /// The <see cref="IStoreSnapshots"/> configuration settings.
        /// </summary>
        IStoreSnapshotSettings SnapshotStore { get; }
    }

    internal sealed class SparkConfigurationSection : ConfigurationSection, ISettings
    {
        [ConfigurationProperty("aggregateStore", IsRequired = false)]
        public AggregateStoreElement AggregateStore { get { return (AggregateStoreElement)base["aggregateStore"] ?? new AggregateStoreElement(); } }
        IStoreAggregateSettings ISettings.AggregateStore { get { return AggregateStore; } }

        [ConfigurationProperty("commandProcessor", IsRequired = false)]
        public CommandProcessorElement CommandProcessor { get { return (CommandProcessorElement)base["commandProcessor"] ?? new CommandProcessorElement(); } }
        IProcessCommandSettings ISettings.CommandProcessor { get { return CommandProcessor; } }

        [ConfigurationProperty("eventStore", IsRequired = false)]
        public EventStoreElement EventStore { get { return (EventStoreElement)base["eventStore"] ?? new EventStoreElement(); } }
        IStoreEventSettings ISettings.EventStore { get { return EventStore; } }

        [ConfigurationProperty("eventProcessor", IsRequired = false)]
        public EventProcessorElement EventProcessor { get { return (EventProcessorElement)base["eventProcessor"] ?? new EventProcessorElement(); } }
        IProcessEventSettings ISettings.EventProcessor { get { return EventProcessor; } }

        [ConfigurationProperty("sagaStore", IsRequired = false)]
        public SagaStoreElement SagaStore { get { return (SagaStoreElement)base["sagaStore"] ?? new SagaStoreElement(); } }
        IStoreSagaSettings ISettings.SagaStore { get { return SagaStore; } }

        [ConfigurationProperty("snapshotStore", IsRequired = false)]
        public SnapshotStoreElement SnapshotStore { get { return (SnapshotStoreElement)base["snapshotStore"] ?? new SnapshotStoreElement(); } }
        IStoreSnapshotSettings ISettings.SnapshotStore { get { return SnapshotStore; } }
    }
}
