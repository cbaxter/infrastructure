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
    namespace UsingObjectHasher
    {
        public class WhenHashingNullValues
        {
            [Fact]
            public void AlwaysReturnConsistentHash()
            {
                Assert.Equal(Guid.Parse("ad85b893-0dfe-89a0-cdf6-34904fd59f71"), ObjectHasher.Hash(null));
            }
        }

        public class WhenHashingPrimitiveValues
        {
            [
            Theory,
            InlineData("8ad85243-aa78-7539-0bf7-0cd6f27bcaa5", 1),
            InlineData("73a742c2-d4b4-8cd4-f8fb-611564291274", 1.1),
            InlineData("84ffd3f1-2943-3277-862d-f21dc4e57262", false)]
            public void AlwaysReturnConsistentHash(String guid, Object value)
            {
                Assert.Equal(Guid.Parse(guid), ObjectHasher.Hash(value));
            }
        }

        public class WhenHashingArrayValues
        {
            [Fact]
            public void CanHashEmptyArray()
            {
                Assert.Equal(Guid.Parse("d98c1dd4-008f-04b2-e980-0998ecf8427e"), ObjectHasher.Hash(new Object[0]));
            }

            [Fact]
            public void CanHashSingleDimensionArray()
            {
                Assert.Equal(Guid.Parse("e1d11d2a-9de5-380a-4c26-951e316cd7e6"), ObjectHasher.Hash(new[] { 1, 2, 3 }));
            }

            [Fact]
            public void CanHashMultiDimensionArray()
            {
                Assert.Equal(Guid.Parse("db536ce4-08dc-5aa1-46b5-c2f5d95a14b0"), ObjectHasher.Hash(new[,] { { 1, 2, 3 }, { 4, 5, 6 } }));
            }

            [Fact]
            public void HashDependentOnValueOrder()
            {
                Assert.NotEqual(ObjectHasher.Hash(new[] { 1, 2, 3 }), ObjectHasher.Hash(new[] { 3, 2, 1 }));
            }
        }

        public class WhenHashingReferenceValues
        {
            [Fact]
            public void DictionaryHashedConsistently()
            {
                var dictionary = new Dictionary<String, Object> { { "Value 1", 1 }, { "Value 2", 2 }, { "Value 3", 3 } };

                Assert.Equal(ObjectHasher.Hash(dictionary), ObjectHasher.Hash(dictionary));
            }

            [Fact]
            public void HashImpactedByDictionaryKeyOrder()
            {
                Assert.NotEqual(
                    ObjectHasher.Hash(new Dictionary<String, Object> { { "Value 2", 2 }, { "Value 3", 3 }, { "Value 1", 1 } }),
                    ObjectHasher.Hash(new Dictionary<String, Object> { { "Value 1", 1 }, { "Value 2", 2 }, { "Value 3", 3 } })
                );
            }

            [Fact]
            public void HashImpactedByReferenceEquality()
            {
                var dict1 = new Dictionary<String, Object> { { "Value 1", 1 }, { "Value 2", 2 }, { "Value 3", 3 } };
                var dict2 = new Dictionary<String, Object> { { "Value 1", 1 }, { "Value 2", 2 }, { "Value 3", 3 } };

                Assert.NotEqual(new { a = dict1, b = dict1 }, new { a = dict1, b = dict2 });
            }

            [Fact]
            public void CircularReferenceDoesNotResultInStackOverflow()
            {
                var dictionary = new Dictionary<String, Object> { { "Value 1", 1 }, { "Value 2", 2 }, { "Value 3", 3 } };

                //NOTE: When `Keys` is accessed, a `KeyCollection` instance is created that holds a reference to the underlying `dictionary` and 
                //      would cause a `StackOverflowException` to be thrown if not handled correctly.
                Assert.NotNull(dictionary.Keys);
                Assert.NotEqual(Guid.Empty, ObjectHasher.Hash(dictionary));
            }
        }

        public class WhenHashingCustomObjects
        {
            [Fact]
            public void IgnoreFieldsMarkedWithNonHashedAttribute()
            {
                var graph = new CustomObject();

                graph.NonHashedField = ObjectHasher.Hash(graph);

                Assert.Equal(graph.NonHashedField, ObjectHasher.Hash(graph));
            }

            [Fact]
            public void HashFieldsWithNonHashedAttributes()
            {
                var graph = new CustomObject();

                graph.NonHashedField = ObjectHasher.Hash(graph);
                graph.AttributeField = Guid.NewGuid();

                Assert.NotEqual(graph.NonHashedField, ObjectHasher.Hash(graph));
            }

            [Fact]
            public void HashFieldsWithNoAttributes()
            {
                var graph = new CustomObject();

                graph.NonHashedField = ObjectHasher.Hash(graph);
                graph.NoAttributeField = Guid.NewGuid();

                Assert.NotEqual(graph.NonHashedField, ObjectHasher.Hash(graph));
            }

            private class CustomObject
            {
                [NonHashed]
                public Guid NonHashedField;

                [DataMember]
                public Guid AttributeField;

                public Guid NoAttributeField;
            }
        }
    }
}
