using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
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

namespace Spark
{
    /// <summary>
    /// Extension methods of <see cref="Type"/>.
    /// </summary>
    public static class TypeExtensions
    {
        private static readonly IDictionary<Type, String> TypeSimpleAssemblyQualifiedName = new ConcurrentDictionary<Type, String>();

        /// <summary>
        /// Returns <value>true</value> if <paramref name="type"/> derives from <paramref name="baseType"/>; otherwise <value>false</value>.
        /// </summary>
        /// <param name="type">The type to test if derives from <paramref name="baseType"/>.</param>
        /// <param name="baseType">The type to test if in <paramref name="type"/> inheritance hierarchy.</param>
        public static Boolean DerivesFrom(this Type type, Type baseType)
        {
            return type.FindBaseType(baseType) != null;
        }

        /// <summary>
        /// Return the type definition if <paramref name="type"/> derives from <paramref name="baseType"/>; otherwise <value>null</value>.
        /// </summary>
        /// <param name="type">The type on which to locate <paramref name="baseType"/>.</param>
        /// <param name="baseType">The target type to find in the type inheritance hierarchy (can be generic type definition).</param>
        public static Type FindBaseType(this Type type, Type baseType)
        {
            Verify.NotNull(type, "type");
            Verify.NotNull(baseType, "baseType");
            Verify.False(baseType.IsInterface, "baseType", Exceptions.TypeArgumentMustNotBeAnInterface);

            var subType = type.BaseType;
            while (subType != null && !Equal(subType, baseType))
                subType = subType.BaseType;

            return subType;
        }

        /// <summary>
        /// Returns true if the specified <paramref name="type"/> is equal to source the <paramref name="targetType"/>.
        /// </summary>
        /// <param name="type">The type to test for equality.</param>
        /// <param name="targetType">The other type to test for equality (can be generic type definition).</param>
        private static Boolean Equal(Type type, Type targetType)
        {
            return targetType.IsGenericTypeDefinition ? type.IsGenericType && type.GetGenericTypeDefinition() == targetType : type == targetType;
        }

        /// <summary>
        /// Get the type hierarchy for the specified <paramref name="type"/> starting with <paramref name="type"/> and ending with <see cref="Object"/>.
        /// </summary>
        /// <param name="type">The type for which the type hierarchy is to be retrieved.</param>
        public static IEnumerable<Type> GetTypeHierarchy(this Type type)
        {
            Verify.NotNull(type, "type");

            yield return type;

            var baseType = type.BaseType;
            while (baseType != null)
            {
                yield return baseType;

                baseType = baseType.BaseType;
            }
        }

        /// <summary>
        /// Get the simple assembly qualified type name (i.e., "System.String, mscorlib" rather than "System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which the short assembly qualified name is to be retrieved.</param>
        public static String GetFullNameWithAssembly(this Type type)
        {
            String result;

            if (!TypeSimpleAssemblyQualifiedName.TryGetValue(type, out result))
                TypeSimpleAssemblyQualifiedName[type] = result = CreateSimpleAssemblyQualifiedName(type.AssemblyQualifiedName);

            return result;
        }

        /// <summary>
        /// Creates the simple assembly qualified type name (i.e., "System.String, mscorlib" rather than "System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089") for a given <paramref name="assemblyQualifiedName"/>.
        /// </summary>
        /// <param name="assemblyQualifiedName">The assembly qualified name from which to strip out version, culture and public key information.</param>
        private static String CreateSimpleAssemblyQualifiedName(String assemblyQualifiedName)
        {
            var typeNameBuilder = new StringBuilder();
            var skippingAssemblyDetails = false;
            var writingAssemblyName = false;
            var current = '\0';

            for (var i = 0; i < assemblyQualifiedName.Length; i++)
            {
                var previous = current;
                switch (current = assemblyQualifiedName[i])
                {
                    case '[': case ']':
                        skippingAssemblyDetails = false;
                        writingAssemblyName = false;
                        break;
                    case ',':
                        if (previous != '[' && previous != ',')
                        {
                            if (!writingAssemblyName)
                                writingAssemblyName = true;
                            else
                                skippingAssemblyDetails = true;
                        }
                        break;
                }

                if (!skippingAssemblyDetails)
                    typeNameBuilder.Append(current);
            }

            return typeNameBuilder.ToString();
        }
    }
}
