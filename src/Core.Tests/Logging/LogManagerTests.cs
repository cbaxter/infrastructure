using Spark;
using Spark.Logging;
using System;
using System.Configuration;
using Xunit;
using Xunit.Extensions;

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

namespace Test.Spark.Logging
{
    public class UsingLogManager
    {
        public class WhenCreatingLogger
        {
            [Fact]
            public void DefaultToWarningIfConfigurationSectionMissing()
            {
                var logManager = new LogManager(null);
                var logger = logManager.CreateLogger("MyTestLogger");

                Assert.True(logger.IsWarnEnabled);
                Assert.False(logger.IsInfoEnabled);
            }

            [Fact]
            public void DefaultToWarningIfSwitchNameIsNull()
            {
                var diagnosticSection = new FakeDiagnosticSectionConfiguration(new FakeSwitchElement(null, "All"));
                var logManager = new LogManager(diagnosticSection);
                var logger = logManager.CreateLogger("MyTestLogger");

                Assert.True(logger.IsWarnEnabled);
                Assert.False(logger.IsInfoEnabled);
            }

            [Fact]
            public void DefaultToWarningIfSwitchValueIsNull()
            {
                var diagnosticSection = new FakeDiagnosticSectionConfiguration(new FakeSwitchElement("default", null));
                var logManager = new LogManager(diagnosticSection);
                var logger = logManager.CreateLogger("MyTestLogger");

                Assert.True(logger.IsWarnEnabled);
                Assert.False(logger.IsInfoEnabled);
            }

            [Fact]
            public void DefaultToWarningIfSwitchValueIsUnknown()
            {
                var diagnosticSection = new FakeDiagnosticSectionConfiguration(new FakeSwitchElement("default", "DoesNotExist"));
                var logManager = new LogManager(diagnosticSection);
                var logger = logManager.CreateLogger("MyTestLogger");

                Assert.True(logger.IsWarnEnabled);
                Assert.False(logger.IsInfoEnabled);
            }

            [Fact]
            public void OverrideDefaultIfSwitchValueIsKnown()
            {
                var diagnosticSection = new FakeDiagnosticSectionConfiguration(new FakeSwitchElement("MyTestLogger", "Information"));
                var logManager = new LogManager(diagnosticSection);
                var logger = logManager.CreateLogger("MyTestLogger");

                Assert.True(logger.IsInfoEnabled);
                Assert.False(logger.IsDebugEnabled);
            }

            [Fact]
            public void TreatPeriodAsNamespaceMarker()
            {
                var diagnosticSection = new FakeDiagnosticSectionConfiguration(new FakeSwitchElement("My", "All"), new FakeSwitchElement("My.Test", "Verbose"));
                var logManager = new LogManager(diagnosticSection);
                var logger = logManager.CreateLogger("My.Test.Logger");

                Assert.True(logger.IsDebugEnabled);
                Assert.False(logger.IsTraceEnabled);
            }

            [Theory, InlineData("My..Test"), InlineData("My.Test."), InlineData(".Test.Logger")]
            public void CanTolerateBadLoggerName(String name)
            {
                var diagnosticSection = new FakeDiagnosticSectionConfiguration(new FakeSwitchElement("default", "Warning"));
                var logManager = new LogManager(diagnosticSection);
                var logger = logManager.CreateLogger(name);

                Assert.True(logger.IsWarnEnabled);
                Assert.False(logger.IsInfoEnabled);
            }

            [Theory, InlineData("MyTest"), InlineData("myTest"), InlineData("MYTEST")]
            public void SwitchNameIsCaseInsensitive(String name)
            {
                var diagnosticSection = new FakeDiagnosticSectionConfiguration(new FakeSwitchElement(name, "Verbose"));
                var logManager = new LogManager(diagnosticSection);
                var logger = logManager.CreateLogger("MyTest");

                Assert.True(logger.IsDebugEnabled);
                Assert.False(logger.IsTraceEnabled);
            }
        }

        public class WhenGettingCurrentClassLogger
        {
            [Fact]
            public void LoggerIsNamedAfterDeclaringClassFullName()
            {
                Assert.Equal("Test.Spark.Logging.UsingLogManager+LoggerClass", LoggerClass.Log.Name);
            }
        }

        public class WhenGettingNamedLogger
        {
            [Fact]
            public void LoggerIsNamedAsSpecified()
            {
                var logger = LogManager.GetLogger("MyCustomName");

                Assert.Equal("MyCustomName", logger.Name);
            }

            [Fact]
            public void LoggerIsNamedAsFullTypeName()
            {
                var logger = LogManager.GetLogger(typeof(WhenGettingNamedLogger));

                Assert.Equal("Test.Spark.Logging.UsingLogManager+WhenGettingNamedLogger", logger.Name);
            }
        }

        public sealed class FakeDiagnosticSectionConfiguration : ConfigurationSection
        {
            public FakeDiagnosticSectionConfiguration(params FakeSwitchElement[] configuredSwitches)
            {
                base["switches"] = new FakeSwitchElementCollection(configuredSwitches);
            }

            [ConfigurationProperty("switches")]
            public FakeSwitchElementCollection Switches { get { return (FakeSwitchElementCollection)base["switches"]; } }
        }

        [ConfigurationCollection(typeof(FakeSwitchElement), CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap)]
        public sealed class FakeSwitchElementCollection : ConfigurationElementCollection
        {
            public FakeSwitchElementCollection()
                : this(null)
            { }

            public FakeSwitchElementCollection(params FakeSwitchElement[] configuredSwitches)
            {
                foreach (var configuredSwitch in configuredSwitches.EmptyIfNull())
                    BaseAdd(configuredSwitch);
            }

            protected override ConfigurationElement CreateNewElement()
            {
                return new FakeSwitchElement();
            }

            protected override Object GetElementKey(ConfigurationElement element)
            {
                return element.GetHashCode();
            }
        }

        public sealed class LoggerClass
        {
            public static readonly ILog Log = LogManager.GetCurrentClassLogger();
        }

        public sealed class FakeSwitchElement : ConfigurationElement
        {
            public FakeSwitchElement()
            { }

            public FakeSwitchElement(String key, String value)
            {
                base["name"] = key;
                base["value"] = value;
            }

            [ConfigurationProperty("name")]
            public String Name { get { return base["name"] as String; } }

            [ConfigurationProperty("value")]
            public String Value { get { return base["value"] as String; } }
        }
    }
}
