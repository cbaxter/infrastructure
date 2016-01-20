using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

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

namespace Spark
{
    /// <summary>
    /// Performs a deep-copy on any object graph.
    /// </summary>
    public static class ObjectCopier
    {
        private static readonly IDictionary<Type, Func<Array, Int32[], Array>> ArrayBuilders = new ConcurrentDictionary<Type, Func<Array, Int32[], Array>>();
        private static readonly IDictionary<Type, Func<Object, IDictionary<Object, Object>, Object>> Copiers = new ConcurrentDictionary<Type, Func<Object, IDictionary<Object, Object>, Object>>();
        private static readonly MethodInfo GetUninitializedObjectMethod = typeof(FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Static | BindingFlags.Public);
        private static readonly MethodInfo CopyObjectMethod = typeof(ObjectCopier).GetMethod("CopyObject", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo SetFieldValueMethod = typeof(FieldInfo).GetMethod("SetValue", new[] { typeof(Object), typeof(Object) });
        private static readonly MethodInfo ArrayLengthMethod = typeof(Array).GetMethod("GetLength", new[] { typeof(Int32) });
        private static readonly MethodInfo DictionaryAddMethod = typeof(IDictionary<Object, Object>).GetMethod("Add");

        /// <summary>
        /// Perform a deep-copy on a non-recursive object graph.
        /// </summary>
        /// <param name="value">The non-recursive object graph from which to create a deep copy.</param>
        public static Object Copy(Object value)
        {
            return CopyObject(value, new Dictionary<Object, Object>());
        }

        /// <summary>
        /// Perform a deep-copy on a non-recursive object graph.
        /// </summary>
        /// <typeparam name="T">The type of object to copy.</typeparam>
        /// <param name="value">The non-recursive object graph from which to create a deep copy.</param>
        public static T Copy<T>(T value)
            where T : class
        {
            return (T)CopyObject(value, new Dictionary<Object, Object>());
        }

        /// <summary>
        /// Determine type of <paramref name="value"/> and process accordingly.
        /// </summary>
        /// <param name="value">The non-recursive object graph from which to create a deep copy.</param>
        /// <param name="visited">The set of visited reference objects.</param>
        private static Object CopyObject(Object value, IDictionary<Object, Object> visited)
        {
            if (value == null)
                return null;

            Type type = value.GetType();
            if (type.IsValueType || typeof(String).IsAssignableFrom(type))
                return value;

            Object copy;
            if (visited.TryGetValue(value, out copy))
                return copy;

            return type.IsArray ? CopyArray((Array)value, visited) : CopyReference(value, visited);
        }

        /// <summary>
        /// Perform a deep copy of an array value.
        /// </summary>
        /// <param name="value">The array from which to create a deep copy.</param>
        /// <param name="visited">The set of visited reference objects.</param>
        private static Object CopyArray(Array value, IDictionary<Object, Object> visited)
        {
            Array copy;
            Type type = value.GetType();
            Func<Array, Int32[], Array> factory;

            if (!ArrayBuilders.TryGetValue(type, out factory))
                ArrayBuilders[type] = factory = CompileArrayCreation(value);

            visited.Add(value, copy = factory.Invoke(value, GetArrayBounds(value)));

            return CopyArrayValues(value, copy, visited);
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
                boundsExpressions.Add(Expression.Call(valueArgument, ArrayLengthMethod, Expression.Constant(i)));

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
        /// <param name="visited">The set of visited reference objects.</param>
        private static Array CopyArrayValues(Array source, Array target, IDictionary<Object, Object> visited)
        {
            if (source.Rank == 1)
            {
                for (var i = 0; i < source.Length; i++)
                    target.SetValue(CopyObject(source.GetValue(i), visited), i);
            }
            else
            {
                var lengths = new Int32[source.Rank];
                for (var dimension = 0; dimension < source.Rank; dimension++)
                    lengths[dimension] = source.GetLength(dimension);

                CopyArrayValues(source, target, lengths, new Int32[source.Rank], 0, visited);
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
        /// <param name="visited">The set of visited reference objects.</param>
        private static void CopyArrayValues(Array source, Array target, Int32[] lengths, Int32[] indicies, Int32 dimension, IDictionary<Object, Object> visited)
        {
            if (dimension == source.Rank - 1)
            {
                indicies[dimension] = 0;
                while (indicies[dimension] < lengths[dimension])
                {
                    target.SetValue(CopyObject(source.GetValue(indicies), visited), indicies);
                    indicies[dimension]++;
                }
            }
            else
            {
                indicies[dimension] = 0;
                while (indicies[dimension] < lengths[dimension])
                {
                    CopyArrayValues(source, target, lengths, indicies, dimension + 1, visited);
                    indicies[dimension]++;
                }
            }
        }

        /// <summary>
        /// Performs a deep copy on a non-recursive reference object.
        /// </summary>
        /// <param name="value">The non-recursive object graph to copy.</param>
        /// <param name="visited">The set of visited reference objects.</param>
        private static Object CopyReference(Object value, IDictionary<Object, Object> visited)
        {
            Type type = value.GetType();
            Func<Object, IDictionary<Object, Object>, Object> factory;

            if (!Copiers.TryGetValue(type, out factory))
                Copiers[type] = factory = CompileObjectMapping(type);

            return factory.Invoke(value, visited);
        }

        /// <summary>
        /// Create an object factory method
        /// </summary>
        /// <param name="type">The object type for which a factory method is to be created.</param>
        private static Func<Object, IDictionary<Object, Object>, Object> CompileObjectMapping(Type type)
        {
            var copy = Expression.Variable(type, "copy");
            var template = Expression.Variable(type, "template");
            var value = Expression.Parameter(typeof(Object), "value");
            var visited = Expression.Parameter(typeof(IDictionary<Object, Object>), "visited");
            var body = Expression.Block(new[] { template, copy }, GetMethodBody(value, template, copy, visited));

            return Expression.Lambda<Func<Object, IDictionary<Object, Object>, Object>>(body, value, visited).Compile();
        }

        /// <summary>
        /// Determins the set of expressions that comprise an object factory method's body.
        /// </summary>
        /// <param name="value">The value argument that represents the object to copy.</param>
        /// <param name="template">The template instance variable reference.</param>
        /// <param name="copied">The copy instance variable reference.</param>
        /// <param name="visited">The visited argument reference.</param>
        private static IEnumerable<Expression> GetMethodBody(ParameterExpression value, ParameterExpression template, ParameterExpression copied, ParameterExpression visited)
        {
            var returnTarget = Expression.Label(template.Type);

            // assign template instance variable to input argument.
            yield return Expression.Assign(template, Expression.Convert(value, template.Type));

            // must use FormatterServices.GetUninitializedObject if no public default ctor (slower).
            if (template.Type.GetConstructor(Type.EmptyTypes) == null)
                yield return Expression.Assign(copied, Expression.Convert(Expression.Call(GetUninitializedObjectMethod, Expression.Constant(template.Type)), template.Type));
            else
                yield return Expression.Assign(copied, Expression.New(template.Type));

            // add the copy to the reference map to ensure that we will not attempt to create a copy more than once.
            yield return Expression.Call(visited, DictionaryAddMethod, value, copied);

            // set all public or private field values in type hierarchy.
            var type = template.Type;
            while (type != typeof(Object) && type != null)
            {
                var fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    // use slower reflection option to set read-only fields (not possible via Expression.Assign).
                    if (field.IsInitOnly)
                        yield return GetImmutableFieldMapping(field, template, copied, visited);
                    else
                        yield return GetMutableFieldMapping(field, template, copied, visited);
                }

                type = type.BaseType;
            }

            // return copied value.
            yield return Expression.Return(returnTarget, copied);
            yield return Expression.Label(returnTarget, Expression.TypeAs(Expression.Constant(null), template.Type));
        }

        /// <summary>
        /// Creates an assignment expression suitable for a mutable object field.
        /// </summary>
        /// <param name="field">The field to be assigned.</param>
        /// <param name="template">The template instance variable reference.</param>
        /// <param name="copy">The copy instance variable reference.</param>
        /// <param name="visited">The visited argument reference.</param>
        private static BinaryExpression GetMutableFieldMapping(FieldInfo field, ParameterExpression template, ParameterExpression copy, ParameterExpression visited)
        {
            return Expression.Assign(
                       Expression.Field(copy, field),
                       Expression.Convert(Expression.Call(CopyObjectMethod, Expression.Convert(Expression.Field(template, field), typeof(Object)), visited), field.FieldType)
                   );
        }

        /// <summary>
        /// Creates an assignment expression suitable for a immutable object field using slower reflection alternative.
        /// </summary>
        /// <param name="field">The field to be assigned.</param>
        /// <param name="template">The template instance variable reference.</param>
        /// <param name="copy">The copy instance variable reference.</param>
        /// <param name="visited">The visited argument reference.</param>
        private static MethodCallExpression GetImmutableFieldMapping(FieldInfo field, ParameterExpression template, ParameterExpression copy, ParameterExpression visited)
        {
            var arguments = new Expression[] { copy, Expression.Call(CopyObjectMethod, Expression.Convert(Expression.Field(template, field), typeof(Object)), visited) };

            return Expression.Call(Expression.Constant(field), SetFieldValueMethod, arguments);
        }
    }
}
