using System;
using System.Collections.Generic;
using System.Linq;
using Spark;
using Xunit;

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

namespace Test.Spark
{
    namespace UsingTypeExtensions
    {
        public class WhenCheckingTypeDerivesFromAnotherType
        {
            [Fact]
            public void TypeCanBeGenericTypeDefinition()
            {
                Assert.True(typeof(CustomList).DerivesFrom(typeof(List<>)));
            }

            [Fact]
            public void ReturnTrueIfTypeFound()
            {
                Assert.True(typeof(CustomList).DerivesFrom(typeof(Object)));
            }

            [Fact]
            public void ReturnFalseIfTypeNotFound()
            {
                Assert.False(typeof(CustomList).DerivesFrom(typeof(Dictionary<,>)));
            }

            private class CustomList : List<Object>
            { }
        }

        public class WhenFindingBaseType
        {
            [Fact]
            public void ReturnTypeWithGenericTypeParameters()
            {
                Assert.Equal(typeof(List<Object>), typeof(CustomList).FindBaseType(typeof(List<>)));
            }

            [Fact]
            public void ReturnNullIfNotFound()
            {
                Assert.Null(typeof(CustomList).FindBaseType(typeof(Dictionary<,>)));
            }

            private class CustomList : List<Object>
            { }
        }

        public class WhenRetrievingTypeHierarchy
        {
            [Fact]
            public void FirstEntryAlwaysCurrentType()
            {
                Assert.Equal(typeof(CustomList), typeof(CustomList).GetTypeHierarchy().First());
            }

            [Fact]
            public void IntermediateEntriesAreBaseTypes()
            {
                Assert.Equal(typeof(List<Object>), typeof(CustomList).GetTypeHierarchy().Skip(1).First());
            }

            [Fact]
            public void LastEntryAlwaysObjectType()
            {
                Assert.Equal(typeof(Object), typeof(CustomList).GetTypeHierarchy().Last());
            }

            private class CustomList : List<Object>
            { }
        }

        public class WhenGettingShortAssemblyQualifiedName
        {
            [
                Theory,
                InlineData(typeof(String), "System.String, mscorlib"),
                InlineData(typeof(String[]), "System.String[], mscorlib"),
                InlineData(typeof(String[,]), "System.String[,], mscorlib"),
                InlineData(typeof(String[][]), "System.String[][], mscorlib"),
                InlineData(typeof(IList<String>), "System.Collections.Generic.IList`1[[System.String, mscorlib]], mscorlib"),
                InlineData(typeof(IList<String>[]), "System.Collections.Generic.IList`1[[System.String, mscorlib]][], mscorlib"),
                InlineData(typeof(IList<String>[,]), "System.Collections.Generic.IList`1[[System.String, mscorlib]][,], mscorlib"),
                InlineData(typeof(IList<String>[, ,]), "System.Collections.Generic.IList`1[[System.String, mscorlib]][,,], mscorlib"),
                InlineData(typeof(IList<String>[][]), "System.Collections.Generic.IList`1[[System.String, mscorlib]][][], mscorlib"),
                InlineData(typeof(IList<IList<String>>), "System.Collections.Generic.IList`1[[System.Collections.Generic.IList`1[[System.String, mscorlib]], mscorlib]], mscorlib"),
                InlineData(typeof(IList<IList<String>[][]>[][]), "System.Collections.Generic.IList`1[[System.Collections.Generic.IList`1[[System.String, mscorlib]][][], mscorlib]][][], mscorlib"),
                InlineData(typeof(IDictionary<String, String>), "System.Collections.Generic.IDictionary`2[[System.String, mscorlib],[System.String, mscorlib]], mscorlib"),
                InlineData(typeof(IDictionary<String, IDictionary<String, String>>), "System.Collections.Generic.IDictionary`2[[System.String, mscorlib],[System.Collections.Generic.IDictionary`2[[System.String, mscorlib],[System.String, mscorlib]], mscorlib]], mscorlib"),
                InlineData(typeof(IDictionary<String, IDictionary<String, IList<String>>>), "System.Collections.Generic.IDictionary`2[[System.String, mscorlib],[System.Collections.Generic.IDictionary`2[[System.String, mscorlib],[System.Collections.Generic.IList`1[[System.String, mscorlib]], mscorlib]], mscorlib]], mscorlib"),
                InlineData(typeof(IDictionary<String, String>[][]), "System.Collections.Generic.IDictionary`2[[System.String, mscorlib],[System.String, mscorlib]][][], mscorlib"),
                InlineData(typeof(IDictionary<String, String>[, ,]), "System.Collections.Generic.IDictionary`2[[System.String, mscorlib],[System.String, mscorlib]][,,], mscorlib"),
                InlineData(typeof(IDictionary<String, String>[,]), "System.Collections.Generic.IDictionary`2[[System.String, mscorlib],[System.String, mscorlib]][,], mscorlib"),
                InlineData(typeof(IDictionary<String, String>[]), "System.Collections.Generic.IDictionary`2[[System.String, mscorlib],[System.String, mscorlib]][], mscorlib"),
                InlineData(typeof(ParentType<Int32>.ChildType<String>.GrantChildType<Boolean>), "Test.Spark.UsingTypeExtensions.WhenGettingShortAssemblyQualifiedName+ParentType`1+ChildType`1+GrantChildType`1[[System.Int32, mscorlib],[System.String, mscorlib],[System.Boolean, mscorlib]], Spark.Core.Tests"),
                InlineData(typeof(ParentType<Int32>.ChildType<String>), "Test.Spark.UsingTypeExtensions.WhenGettingShortAssemblyQualifiedName+ParentType`1+ChildType`1[[System.Int32, mscorlib],[System.String, mscorlib]], Spark.Core.Tests"),
                InlineData(typeof(ParentType<Int32>.ChildType), "Test.Spark.UsingTypeExtensions.WhenGettingShortAssemblyQualifiedName+ParentType`1+ChildType[[System.Int32, mscorlib]], Spark.Core.Tests"),
                InlineData(typeof(ParentType.ChildType<String>), "Test.Spark.UsingTypeExtensions.WhenGettingShortAssemblyQualifiedName+ParentType+ChildType`1[[System.String, mscorlib]], Spark.Core.Tests"),
                InlineData(typeof(ParentType.ChildType), "Test.Spark.UsingTypeExtensions.WhenGettingShortAssemblyQualifiedName+ParentType+ChildType, Spark.Core.Tests"),
            ]
            public void ReturnTypeNameWithNoCultureVersionOrPublicKeys(Type type, String expectedTypeName)
            {
                var simpleAssemblyQualifiedName = type.GetFullNameWithAssembly();

                Assert.Equal(expectedTypeName, simpleAssemblyQualifiedName);
                Assert.NotNull(Type.GetType(simpleAssemblyQualifiedName, throwOnError: true));
            }

            // ReSharper disable UnusedTypeParameter
            internal class ParentType
            {
                internal class ChildType
                {

                }

                internal class ChildType<T>
                {

                }
            }

            internal class ParentType<TParent>
            {
                internal class ChildType
                {

                }

                internal class ChildType<TChild>
                {
                    internal class GrantChildType<TGrandChild>
                    {

                    }
                }
            }
            // ReSharper restore UnusedTypeParameter
        }
    }
}
