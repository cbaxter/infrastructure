using System.Configuration;
using Spark.Cqrs.Eventing.Sagas;
using Spark.EventStore;

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

namespace Spark.Configuration
{
    /// <summary>
    /// Static settings class to access `spark.infrastructure` configuration section.
    /// </summary>
    internal static class Settings
    {
        private static readonly ISettings configuration;

        /// <summary>
        /// Gets the default application <see cref="ISettings"/>.
        /// </summary>
        public static ISettings Default { get { return configuration; } }

        /// <summary>
        /// Initializes <see cref="configuration"/> to the currently configured <see cref="SparkConfigurationSection"/>.
        /// </summary>
        static Settings()
        {
            configuration = (SparkConfigurationSection)ConfigurationManager.GetSection("spark.infrastructure") ?? new SparkConfigurationSection();
        }

        /// <summary>
        /// The <see cref="CommandProcessor"/> configuration settings.
        /// </summary>
        public static IStoreAggregateSettings AggregateStore
        {
            get { return configuration.AggregateStore; }
        }

        /// <summary>
        /// The <see cref="CommandProcessor"/> configuration settings.
        /// </summary>
        public static IProcessCommandSettings CommandProcessor
        {
            get { return configuration.CommandProcessor; }
        }

        /// <summary>
        /// The <see cref="EventStore"/> configuration settings.
        /// </summary>
        public static IStoreEventSettings EventStore
        {
            get { return configuration.EventStore; }
        }

        /// <summary>
        /// The <see cref="EventProcessor"/> configuration settings.
        /// </summary>
        public static IProcessEventSettings EventProcessor
        {
            get { return configuration.EventProcessor; }
        }

        /// <summary>
        /// The <see cref="IStoreSagas"/> configuration settings.
        /// </summary>
        public static IStoreSagaSettings SagaStore
        {
            get { return configuration.SagaStore; }
        }

        /// <summary>
        /// The <see cref="IStoreSnapshots"/> configuration settings.
        /// </summary>
        public static IStoreSnapshotSettings SnapshotStore
        {
            get { return configuration.SnapshotStore; }
        }
    }
}
