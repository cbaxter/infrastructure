using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Spark;
using Xunit;

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

namespace Test.Spark
{
    public static class UsingObjectMapper
    {
        public class WhenGettingObjectState
        {
            [Fact]
            public void WillIgnoreReadOnlyFields()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.False(map.ContainsKey("ReadOnlyField"));
            }

            [Fact]
            public void WillMapPublicFields()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.Equal("PublicField", map["PublicField"]);
            }

            [Fact]
            public void WillMapPublicProperties()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.Equal("PublicAutoProperty", map["PublicAutoProperty"]);
            }

            [Fact]
            public void WillMapNonPublicFields()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.Equal("PrivateField", map["PrivateField"]);
            }

            [Fact]
            public void WillMapNonPublicProperties()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.Equal("PrivateAutoProperty", map["PrivateAutoProperty"]);
            }

            [Fact]
            public void WillIgnoreFieldsMarkedWithIgnoreAttribute()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.False(map.ContainsKey("IgnoredField"));
            }

            [Fact]
            public void WillIgnorePropertiesMarkedWithIgnoreAttribute()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.False(map.ContainsKey("IgnoredProperty"));
            }

            [Fact]
            public void WillUseFieldDataMemberAttributeIfDefined()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.Equal("CustomFieldName", map["cfn"]);
            }

            [Fact]
            public void WillUsePropertyDataMemberAttributeIfDefined()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.Equal("CustomAutoPropertyName", map["cpn"]);
            }

            [Fact]
            public void WillIncludeBaseClassFields()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.Equal("BaseField", map["BaseField"]);
            }

            [Fact]
            public void WillIncludeBaseClassProperties()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.Equal("BaseProperty", map["BaseProperty"]);
            }

            [Fact]
            public void WillNotSerializeDefaultValueByDefault()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.False(map.ContainsKey("ExcludeDefaultvalue"));
            }

            [Fact]
            public void WillSerializeDefaultValueIfRequested()
            {
                var instance = new DerivedClass();
                var map = ObjectMapper.GetState(instance);

                Assert.Equal(0, map["IncludeDefaultvalue"]);
            }
        }

        public class WhenSettingObjectState
        {
            [Fact]
            public void WillRestorePublicFieldValue()
            {
                var instance = new DerivedClass();
                var value = Guid.NewGuid().ToString();
                var state = new Dictionary<String, Object> { { "PublicField", value } };

                ObjectMapper.SetState(instance, state);

                Assert.Equal(value, instance.PublicField);
            }

            [Fact]
            public void WillRestorePublicPropertyValue()
            {
                var instance = new DerivedClass();
                var value = Guid.NewGuid().ToString();
                var state = new Dictionary<String, Object> { { "PublicAutoProperty", value } };

                ObjectMapper.SetState(instance, state);

                Assert.Equal(value, instance.PublicAutoProperty);
            }

            [Fact]
            public void WillRestoreNonPublicFieldValue()
            {
                var instance = new DerivedClass();
                var value = Guid.NewGuid().ToString();
                var state = new Dictionary<String, Object> { { "PrivateField", value } };

                ObjectMapper.SetState(instance, state);

                Assert.Equal(value, instance.GetPrivateFieldValue);
            }

            [Fact]
            public void WillRestoreNonPublicPropertyValue()
            {
                var instance = new DerivedClass();
                var value = Guid.NewGuid().ToString();
                var state = new Dictionary<String, Object> { { "PrivateAutoProperty", value } };

                ObjectMapper.SetState(instance, state);

                Assert.Equal(value, instance.GetPrivateAutoPropertyValue);
            }

            [Fact]
            public void WillRestoreBaseFieldValue()
            {
                var instance = new DerivedClass();
                var value = Guid.NewGuid().ToString();
                var state = new Dictionary<String, Object> { { "BaseField", value } };

                ObjectMapper.SetState(instance, state);

                Assert.Equal(value, instance.GetBaseFieldValue);
            }

            [Fact]
            public void WillRestoreBasePropertyValue()
            {
                var instance = new DerivedClass();
                var value = Guid.NewGuid().ToString();
                var state = new Dictionary<String, Object> { { "BaseProperty", value } };

                ObjectMapper.SetState(instance, state);

                Assert.Equal(value, instance.GetBasePropertyValue);
            }

            [Fact]
            public void WillRestoreCustomFieldNameValue()
            {
                var instance = new DerivedClass();
                var value = Guid.NewGuid().ToString();
                var state = new Dictionary<String, Object> { { "cfn", value } };

                ObjectMapper.SetState(instance, state);

                Assert.Equal(value, instance.CustomFieldName);
            }

            [Fact]
            public void WillRestoreCustomPropertyNameValue()
            {
                var instance = new DerivedClass();
                var value = Guid.NewGuid().ToString();
                var state = new Dictionary<String, Object> { { "cpn", value } };

                ObjectMapper.SetState(instance, state);

                Assert.Equal(value, instance.CustomAutoPropertyName);
            }
        }

        public class BaseClass
        {
            private String BaseField = "BaseField";

            public String GetBaseFieldValue { get { return BaseField; } }

            private String BaseProperty { get; set; }

            public String GetBasePropertyValue { get { return BaseProperty; } }

            public BaseClass()
            {
                BaseProperty = "BaseProperty";
            }
        }

        public class DerivedClass : BaseClass
        {
            public readonly String ReadOnlyField = "ReadOnlyField";

            public String PublicField = "PublicField";

            public String PublicAutoProperty { get; set; }

            private String PrivateField = "PrivateField";

            public String GetPrivateFieldValue { get { return PrivateField; } }

            private String PrivateAutoProperty { get; set; }

            public String GetPrivateAutoPropertyValue { get { return PrivateAutoProperty; } }

            [IgnoreDataMember]
            public String IgnoredField = "IgnoredField";

            [IgnoreDataMember]
            public String IgnoredAutoProperty { get; set; }

            [DataMember(Name = "cfn")]
            public String CustomFieldName = "CustomFieldName";

            [DataMember(Name = "cpn")]
            public String CustomAutoPropertyName { get; set; }

            public Int32 ExcludeDefaultvalue { get; set; }

            [DataMember(EmitDefaultValue = true)]
            public Int32 IncludeDefaultvalue { get; set; }

            public DerivedClass()
            {
                PublicAutoProperty = "PublicAutoProperty";
                PrivateAutoProperty = "PrivateAutoProperty";
                IgnoredAutoProperty = "IgnoredAutoProperty";
                CustomAutoPropertyName = "CustomAutoPropertyName";
            }
        }
    }
}
