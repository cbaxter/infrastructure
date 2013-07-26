using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

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
    /// Performs a deep-copy on any non-recursive object graph.
    /// </summary>
    public static class ObjectCopier
    {
        private static readonly IDictionary<Type, Func<Object, Object>> Copiers = new ConcurrentDictionary<Type, Func<Object, Object>>();
        private static readonly IDictionary<Type, Func<Array, Int32[], Array>> ArrayBuilders = new ConcurrentDictionary<Type, Func<Array, Int32[], Array>>();
        private static readonly MethodInfo GetUninitializedObjectMethod = typeof(FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Static | BindingFlags.Public);
        private static readonly MethodInfo CopyObjectMethod = typeof(ObjectCopier).GetMethod("CopyObject", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo SetFieldValueMethod = typeof(FieldInfo).GetMethod("SetValue", new[] { typeof(Object), typeof(Object) });
        private static readonly MethodInfo ArrayLengthMethod = typeof(Array).GetMethod("GetLength", new[] { typeof(Int32) });

        /// <summary>
        /// Perform a deep-copy on a non-recursive object graph.
        /// </summary>
        /// <param name="value">The non-recursive object graph from which to create a deep copy.</param>
        public static Object Copy(Object value)
        {
            return CopyObject(value);
        }

        /// <summary>
        /// Perform a deep-copy on a non-recursive object graph.
        /// </summary>
        /// <typeparam name="T">The type of object to copy.</typeparam>
        /// <param name="value">The non-recursive object graph from which to create a deep copy.</param>
        public static T Copy<T>(T value)
            where T : class
        {
            return (T)CopyObject(value);
        }

        /// <summary>
        /// Determine type of <paramref name="value"/> and process accordingly.
        /// </summary>
        /// <param name="value">The non-recursive object graph from which to create a deep copy.</param>
        private static Object CopyObject(Object value)
        {
            if (value == null)
                return null;

            var type = value.GetType();
            if (type.IsValueType || typeof(String).IsAssignableFrom(type))
                return value;

            return type.IsArray ? CopyArray((Array)value) : CopyReference(value);
        }

        /// <summary>
        /// Perform a deep copy of an array value.
        /// </summary>
        /// <param name="value">The array from which to create a deep copy.</param>
        private static Object CopyArray(Array value)
        {
            var type = value.GetType();

            Func<Array, Int32[], Array> factory;
            if (!ArrayBuilders.TryGetValue(type, out factory))
                ArrayBuilders[type] = factory = CompileArrayCreation(value);

            return CopyArrayValues(value, factory.Invoke(value, GetArrayBounds(value)));
        }

        /// <summary>
        /// Create an array factory method for the specified array <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The array for which a factory method must be created.</param>
        private static Func<Array, Int32[], Array> CompileArrayCreation(Array value)
        {
            var valueArgument = Expression.Parameter(typeof(Array), "value");
            var boundsArgument = Expression.Parameter(typeof(Int32[]), "bounds");
            var boundsExpressions = new List<Expression>();
            var type = value.GetType().GetElementType();

            for (var i = 0; i < value.Rank; i++)
                boundsExpressions.Add(Expression.Call(valueArgument, ArrayLengthMethod, new Expression[] { Expression.Constant(i) }));

            return Expression.Lambda<Func<Array, Int32[], Array>>(Expression.NewArrayBounds(type, boundsExpressions), valueArgument, boundsArgument).Compile();
        }

        /// <summary>
        /// Get the bounds of the current array <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The array value for which the bounds are to be retrieved.</param>
        private static Int32[] GetArrayBounds(Array value)
        {
            var bounds = new Int32[value.Rank];
            for (var i = 0; i < value.Rank; i++)
                bounds[i] = value.GetLength(i);

            return bounds;
        }

        /// <summary>
        /// Performs a deep-copy on the array values.
        /// </summary>
        /// <param name="source">The source array.</param>
        /// <param name="target">The target array.</param>
        private static Array CopyArrayValues(Array source, Array target)
        {
            if (source.Rank == 1)
            {
                for (var i = 0; i < source.Length; i++)
                    target.SetValue(CopyObject(source.GetValue(i)), i);
            }
            else
            {
                var lengths = new Int32[source.Rank];
                for (var dimension = 0; dimension < source.Rank; dimension++)
                    lengths[dimension] = source.GetLength(dimension);

                CopyArrayValues(source, target, lengths, new Int32[source.Rank], 0);
            }

            return target;
        }

        /// <summary>
        /// Performs a deep-copy on the array values for a multi-dimensional array.
        /// </summary>
        /// <param name="source">The source array.</param>
        /// <param name="target">The target array.</param>
        /// <param name="lengths">The set of array lengths that make up the multi-dimensional array.</param>
        /// <param name="indicies">The indicies of the multi-dimensional array to be processed.</param>
        /// <param name="dimension">The current array dimension being processed.</param>
        private static void CopyArrayValues(Array source, Array target, Int32[] lengths, Int32[] indicies, Int32 dimension)
        {
            if (dimension == source.Rank - 1)
            {
                indicies[dimension] = 0;
                while (indicies[dimension] < lengths[dimension])
                {
                    target.SetValue(source.GetValue(indicies), indicies);
                    indicies[dimension]++;
                }
            }
            else
            {
                indicies[dimension] = 0;
                while (indicies[dimension] < lengths[dimension])
                {
                    CopyArrayValues(source, target, lengths, indicies, dimension + 1);
                    indicies[dimension]++;
                }
            }
        }

        /// <summary>
        /// Performs a deep copy on a non-recursive reference object.
        /// </summary>
        /// <param name="value">The non-recursive object graph to copy.</param>
        private static Object CopyReference(Object value)
        {
            var type = value.GetType();

            Func<Object, Object> factory;
            if (!Copiers.TryGetValue(type, out factory))
                Copiers[type] = factory = CompileObjectMapping(type);

            return factory.Invoke(value);
        }

        /// <summary>
        /// Create an object factory method
        /// </summary>
        /// <param name="type">The object type for which a factory method is to be created.</param>
        private static Func<Object, Object> CompileObjectMapping(Type type)
        {
            var argument = Expression.Parameter(typeof(Object), "value");
            var templateInstance = Expression.Variable(type, "template");
            var copiedInstance = Expression.Variable(type, "copy");

            return Expression.Lambda<Func<Object, Object>>(Expression.Block(new[] { templateInstance, copiedInstance }, GetMethodBody(argument, templateInstance, copiedInstance)), argument).Compile();
        }

        /// <summary>
        /// Determins the set of expressions that comprise an object factory method's body.
        /// </summary>
        /// <param name="argument">The value argument that represents the object to copy.</param>
        /// <param name="templateInstance">The template instance variable reference.</param>
        /// <param name="copiedInstance">The copy instance variable reference.</param>
        private static IEnumerable<Expression> GetMethodBody(ParameterExpression argument, ParameterExpression templateInstance, ParameterExpression copiedInstance)
        {
            var returnTarget = Expression.Label(templateInstance.Type);

            // assign template instance variable to input argument.
            yield return Expression.Assign(templateInstance, Expression.Convert(argument, templateInstance.Type));

            // must use FormatterServices.GetUninitializedObject if no public default ctor (slower).
            if (templateInstance.Type.GetConstructor(Type.EmptyTypes) == null)
                yield return Expression.Assign(copiedInstance, Expression.Convert(Expression.Call(GetUninitializedObjectMethod, Expression.Constant(templateInstance.Type)), templateInstance.Type));
            else
                yield return Expression.Assign(copiedInstance, Expression.New(templateInstance.Type));

            // set all public or private field values in type hierarchy.
            var type = templateInstance.Type;
            while (type != typeof(Object) && type != null)
            {
                var fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    // use slower reflection option to set read-only fields (not possible via Expression.Assign).
                    if (field.IsInitOnly)
                        yield return GetImmutableFieldMapping(field, templateInstance, copiedInstance);
                    else
                        yield return GetMutableFieldMapping(field, templateInstance, copiedInstance);
                }

                type = type.BaseType;
            }

            // return copied value.
            yield return Expression.Return(returnTarget, copiedInstance);
            yield return Expression.Label(returnTarget, Expression.TypeAs(Expression.Constant(null), templateInstance.Type));
        }

        /// <summary>
        /// Creates an assignment expression suitable for a mutable object field.
        /// </summary>
        /// <param name="field">The field to be assigned.</param>
        /// <param name="templateInstance">The template instance variable reference.</param>
        /// <param name="copiedInstance">The copy instance variable reference.</param>
        private static BinaryExpression GetMutableFieldMapping(FieldInfo field, ParameterExpression templateInstance, ParameterExpression copiedInstance)
        {
            return Expression.Assign(
                       Expression.Field(copiedInstance, field),
                       Expression.Convert(Expression.Call(CopyObjectMethod, Expression.Convert(Expression.Field(templateInstance, field), typeof(Object))), field.FieldType)
                   );
        }

        /// <summary>
        /// Creates an assignment expression suitable for a immutable object field using slower reflection alternative.
        /// </summary>
        /// <param name="field">The field to be assigned.</param>
        /// <param name="templateInstance">The template instance variable reference.</param>
        /// <param name="copiedInstance">The copy instance variable reference.</param>
        private static MethodCallExpression GetImmutableFieldMapping(FieldInfo field, ParameterExpression templateInstance, ParameterExpression copiedInstance)
        {
            var arguments = new Expression[] { copiedInstance, Expression.Call(CopyObjectMethod, Expression.Convert(Expression.Field(templateInstance, field), typeof(Object))) };

            return Expression.Call(Expression.Constant(field), SetFieldValueMethod, arguments);
        }
    }
}
