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
        private static readonly LogManager Instance = new LogManager(ConfigurationManager.GetSection("system.diagnostics") as ConfigurationSection);
        private readonly IReadOnlyDictionary<String, SourceLevels> configuredSwitches;
        private readonly SourceLevels defaultLevel;

        /// <summary>
        /// Initializes the configured switches for <see cref="LogManager"/>.
        /// </summary>
        /// <param name="diagnosticsSection">The system.diagnostics configuration section.</param>
        internal LogManager(ConfigurationSection diagnosticsSection)
        {
            var switchSection = diagnosticsSection != null ? diagnosticsSection.ElementInformation.Properties["switches"] : null;
            var switches = switchSection != null && switchSection.Value is IEnumerable ? (IEnumerable)switchSection.Value : Enumerable.Empty<ConfigurationElement>();

            configuredSwitches = GetConfiguredSwitches(switches.OfType<ConfigurationElement>());
            if (!configuredSwitches.TryGetValue("default", out defaultLevel))
                defaultLevel = SourceLevels.Warning;
        }

        /// <summary>
        /// Get the configured trace switches from the application configuration file.
        /// </summary>
        /// <param name="switchElements">The collection of <see cref="ConfigurationElement"/> instances to attempt to parse the <see cref="SourceLevels"/> value.</param>
        private static IReadOnlyDictionary<String, SourceLevels> GetConfiguredSwitches(IEnumerable<ConfigurationElement> switchElements)
        {
            var configuredSwitches = new Dictionary<String, SourceLevels>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var element in switchElements)
            {
                var level = default(SourceLevels);
                var name = GetPropertyValue(element, "name");
                var value = GetPropertyValue(element, "value");

                if (String.IsNullOrWhiteSpace(name))
                    continue;

                if (!Enum.TryParse(value, true, out level))
                    continue;

                configuredSwitches[name] = level;
            }

            return new ReadOnlyDictionary<String, SourceLevels>(configuredSwitches);
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
            Verify.NotNullOrWhiteSpace(name, "name");

            var switchName = name;
            do
            {
                if (configuredSwitches.ContainsKey(switchName))
                    return new Logger(name, configuredSwitches[switchName]);

                // Use `.` as a hierarchical separator and travel up the name looking for a match.
                switchName = switchName.Substring(0, Math.Max(0, switchName.LastIndexOf('.')));
            } while (!String.IsNullOrWhiteSpace(switchName));

            return new Logger(name, defaultLevel);
        }

        /// <summary>
        /// Gets the specified named <see cref="ILog"/> instance.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        public static ILog GetLogger(String name)
        {
            return Instance.CreateLogger(name);
        }

        /// <summary>
        /// Gets a <see cref="ILog"/> instance named after the caller's declaring or reflected type.
        /// </summary>
        public static ILog GetCurrentClassLogger()
        {
            var caller = new StackFrame(1, false).GetMethod();
            var name = (caller.DeclaringType ?? caller.ReflectedType).FullName;

            return Instance.CreateLogger(name);
        }
    }
}
