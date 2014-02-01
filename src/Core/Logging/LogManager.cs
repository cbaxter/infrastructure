using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

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

namespace Spark.Logging
{
    /// <summary>
    /// Creates new instances of <see cref="ILog"/> objects.
    /// </summary>
    public sealed class LogManager
    {
        private static readonly LogManager Instance = new LogManager();
        private readonly IDictionary<String, Logger> cachedLoggers = new Dictionary<String, Logger>();
        private readonly IReadOnlyDictionary<String, SourceSwitch> configuredSwitches;
        private readonly IReadOnlyDictionary<String, TraceSource> configuredSources;
        private readonly SourceLevels defaultLevel;

        /// <summary>
        /// Initializes the configured switches for <see cref="LogManager"/>.
        /// </summary>
        public LogManager()
            : this(ConfigurationManager.GetSection("system.diagnostics") as ConfigurationSection)
        { }

        /// <summary>
        /// Initializes the configured switches for <see cref="LogManager"/>.
        /// </summary>
        /// <param name="diagnosticsSection">The system.diagnostics configuration section.</param>
        internal LogManager(ConfigurationSection diagnosticsSection)
        {
            SourceSwitch defaultSwitch;

            configuredSources = GetConfiguredSources(diagnosticsSection);
            configuredSwitches = GetConfiguredSwitches(diagnosticsSection);
            defaultLevel = configuredSwitches.TryGetValue("default", out defaultSwitch) ? defaultSwitch.Level : SourceLevels.Warning;
        }

        /// <summary>
        /// Get the configured trace switches from the application configuration file.
        /// </summary>
        /// <param name="diagnosticsSection">The system.diagnostics configuration section.</param>
        private static IReadOnlyDictionary<String, SourceSwitch> GetConfiguredSwitches(ConfigurationSection diagnosticsSection)
        {
            var switchSection = diagnosticsSection != null ? diagnosticsSection.ElementInformation.Properties["switches"] : null;
            var switches = switchSection != null && switchSection.Value is IEnumerable ? (IEnumerable)switchSection.Value : Enumerable.Empty<ConfigurationElement>();
            var configuredSwitches = new Dictionary<String, SourceSwitch>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var name in switches.OfType<ConfigurationElement>().Select(element => GetPropertyValue(element, "name")).Where(name => name.IsNotNullOrWhiteSpace()))
                configuredSwitches[name] = new SourceSwitch(name);

            return new ReadOnlyDictionary<String, SourceSwitch>(configuredSwitches);
        }

        /// <summary>
        /// Get the configured trace sources from the application configuration file.
        /// </summary>
        /// <param name="diagnosticsSection">The system.diagnostics configuration section.</param>
        private static IReadOnlyDictionary<String, TraceSource> GetConfiguredSources(ConfigurationSection diagnosticsSection)
        {
            var sourceSection = diagnosticsSection != null ? diagnosticsSection.ElementInformation.Properties["sources"] : null;
            var sources = sourceSection != null && sourceSection.Value is IEnumerable ? (IEnumerable)sourceSection.Value : Enumerable.Empty<ConfigurationElement>();
            var configuredSources = new Dictionary<String, TraceSource>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var name in sources.OfType<ConfigurationElement>().Select(element => GetPropertyValue(element, "name")).Where(name => name.IsNotNullOrWhiteSpace()))
                configuredSources[name] = new TraceSource(name);

            return new ReadOnlyDictionary<String, TraceSource>(configuredSources);
        }

        /// <summary>
        /// Gets a <see cref="ConfigurationElement"/> property value.
        /// </summary>
        /// <param name="element">The <see cref="ConfigurationElement"/> on which the desired property is defined.</param>
        /// <param name="name">The <see cref="String"/> name of the property to access on <paramref name="element"/>.</param>
        private static String GetPropertyValue(ConfigurationElement element, String name)
        {
            var property = element.ElementInformation.Properties[name];

            return property == null ? String.Empty : property.Value as String ?? String.Empty;
        }

        /// <summary>
        /// Gets the specified named <see cref="ILog"/> instance.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        internal ILog CreateLogger(String name)
        {
            Logger logger;

            lock (cachedLoggers)
            {
                if (!cachedLoggers.TryGetValue(name, out logger))
                    cachedLoggers[name] = logger = new Logger(name, GetLevel(name), GetListeners(name));
            }

            return logger;
        }

        /// <summary>
        /// Get the source level associated with the specified logger <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        private SourceLevels GetLevel(String name)
        {
            SourceSwitch sourceSwitch;

            do
            {
                // Use the configured source switch level if exists.
                if (configuredSwitches.TryGetValue(name, out sourceSwitch))
                    return sourceSwitch.Level;

                // Use `.` as a hierarchical separator and travel up the name looking for a match.
                name = name.Substring(0, Math.Max(0, name.LastIndexOf('.')));
            } while (name.IsNotNullOrWhiteSpace());

            return defaultLevel;
        }

        /// <summary>
        /// Get the trace listener(s) associated with the specified logger <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        private TraceListenerCollection GetListeners(String name)
        {
            TraceSource traceSource;

            do
            {
                // Use the configured source switch level if exists.
                if (configuredSources.TryGetValue(name, out traceSource))
                    return traceSource.Listeners;

                // Use `.` as a hierarchical separator and travel up the name looking for a match.
                name = name.Substring(0, Math.Max(0, name.LastIndexOf('.')));
            } while (name.IsNotNullOrWhiteSpace());

            return Trace.Listeners;
        }

        /// <summary>
        /// Gets the specified named <see cref="ILog"/> instance.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        public static ILog GetLogger(String name)
        {
            Verify.NotNullOrWhiteSpace(name, "name");

            return Instance.CreateLogger(name);
        }

        /// <summary>
        /// Gets the <see cref="ILog"/> instance associated with the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type associated with the logger.</param>
        public static ILog GetLogger(Type type)
        {
            Verify.NotNull(type, "type");

            return Instance.CreateLogger(type.FullName);
        }

        /// <summary>
        /// Gets a <see cref="ILog"/> instance named after the caller's declaring or reflected type.
        /// </summary>
        public static ILog GetCurrentClassLogger()
        {
            var caller = new StackFrame(1, false).GetMethod();
            var name = (caller.DeclaringType ?? caller.ReflectedType ?? typeof(UnknownLogger)).FullName;

            return Instance.CreateLogger(name);
        }

        /// <summary>
        /// An unknown logger class.
        /// </summary>
        private sealed class UnknownLogger { }
    }
}
