using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

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
    /// Performs a shallow mapping to/from an object graph/dictionary.
    /// </summary>
    /// <remarks>
    /// Mapping controlled by <see cref="DataMemberAttribute"/> and/or <see cref="IgnoreDataMemberAttribute"/> attributes on specific properties or fields.
    /// </remarks>
    public static class ObjectMapper
    {
        private static readonly MethodInfo DictionaryTryGetValueMethod = typeof(IDictionary<String, Object>).GetMethod("TryGetValue");
        private static readonly MethodInfo DictionaryAddMethod = typeof(IDictionary<String, Object>).GetMethod("Add", new[] { typeof(String), typeof(Object) });
        private static readonly ConstructorInfo MissingMemberExceptionConstructor = typeof(MissingMemberException).GetConstructor(new[] { typeof(String), typeof(String) });
        private static readonly ConstructorInfo DictionaryConstructor = typeof(Dictionary<String, Object>).GetConstructor(new[] { typeof(Int32) });
        private static readonly IDictionary<Type, ObjectBinding> Bindings = new ConcurrentDictionary<Type, ObjectBinding>();
        
        /// <summary>
        /// Get the field type for the specified <paramref name="type"/> attribute <paramref name="name"/>.
        /// </summary>
        /// <param name="type">The type of the object.</param>
        /// <param name="name">The name of the field.</param>
        public static Type GetFieldType(Type type, String name)
        {
            Verify.NotNull(type, "type");
            Verify.NotNullOrWhiteSpace(name, "name");

            return GetBinding(type).GetFieldType(name);
        }

        /// <summary>
        /// Get the underlying <see cref="Object"/> mutable state information.
        /// </summary>
        /// <param name="value">The object graph for which fields/properties are to be mapped.</param>
        public static IDictionary<String, Object> GetState(Object value)
        {
            Verify.NotNull(value, "value");

            return GetBinding(value.GetType()).GetState(value);
        }

        /// <summary>
        /// Set the underlying <see cref="Object"/> mutable state information.
        /// </summary>
        /// <param name="value">The object graph for which fields/properties are to be mapped.</param>
        /// <param name="state">The source state to be mapped on to <paramref name="value"/>.</param>
        public static void SetState(Object value, IDictionary<String, Object> state)
        {
            Verify.NotNull(value, "value");
            Verify.NotNull(state, "state");

            GetBinding(value.GetType()).SetState(value, state);
        }
        
        /// <summary>
        /// Get the object binding for the specified object <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of object for which bindings are to be compiled.</param>
        private static ObjectBinding GetBinding(Type type)
        {
            ObjectBinding binding;

            if (!Bindings.TryGetValue(type, out binding))
                Bindings[type] = binding = CompileBindings(type);

            return binding;
        }

        /// <summary>
        /// Compiles the get and set method bindings for a specific object <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type of object for which bindings are to be compiled.</param>
        private static ObjectBinding CompileBindings(Type type)
        {
            var fields = GetFieldBindings(type).Where(binding => !binding.Ignore).OrderBy(binding => binding.Metadata.Order).ThenBy(binding => binding.Metadata.Name).ToArray();

            return new ObjectBinding(fields, CompileGetStateMethod(type, fields), CompileSetStateMethod(type, fields));
        }

        /// <summary>
        /// Get the underlying field information for the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type for which all field bindings are to be retrieved.</param>
        private static IEnumerable<FieldBinding> GetFieldBindings(Type type)
        {
            var baseType = type;
            var fields = new List<FieldInfo>();
            while (baseType != typeof(Object) && baseType != null)
            {
                fields.AddRange(baseType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                baseType = baseType.BaseType;
            }

            return fields.Select(fieldInfo => new FieldBinding(fieldInfo));
        }

        /// <summary>
        /// Compile the data extractor method for a given object <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type of object for which bindings are to be compiled.</param>
        /// <param name="bindings">The set of mutable field bindings that are to be mapped.</param>
        private static Func<Object, IDictionary<String, Object>> CompileGetStateMethod(Type type, FieldBinding[] bindings)
        {
            var methodBody = new List<Expression>();
            var value = Expression.Parameter(typeof(Object), "value");
            var result = Expression.Label(typeof(IDictionary<String, Object>));
            var state = Expression.Variable(typeof(IDictionary<String, Object>), "state");
            var source = Expression.Variable(type, "source");

            methodBody.Add(Expression.Assign(state, Expression.New(DictionaryConstructor, Expression.Constant(bindings.Length))));
            methodBody.Add(Expression.Assign(source, Expression.Convert(value, type)));

            foreach (var binding in bindings)
            {
                var expression = Expression.Call(state, DictionaryAddMethod, Expression.Constant(binding.Metadata.Name), Expression.Convert(Expression.Field(source, binding.Field), typeof(Object)));

                if (binding.Metadata.EmitDefaultValue)
                    methodBody.Add(expression);
                else
                    methodBody.Add(Expression.IfThen(Expression.NotEqual(Expression.Field(source, binding.Field), Expression.Default(binding.Field.FieldType)), expression));
            }

            methodBody.Add(Expression.Return(result, state));
            methodBody.Add(Expression.Label(result, Expression.TypeAs(Expression.Constant(null), state.Type)));

            return Expression.Lambda<Func<Object, IDictionary<String, Object>>>(Expression.Block(new[] { state, source }, methodBody), value).Compile();
        }

        /// <summary>
        /// Compile the data assignment method for a given object <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type of object for which bindings are to be compiled.</param>
        /// <param name="bindings">The set of mutable field bindings that are to be mapped.</param>
        private static Action<Object, IDictionary<String, Object>> CompileSetStateMethod(Type type, FieldBinding[] bindings)
        {
            var methodBody = new List<Expression>();
            var value = Expression.Parameter(typeof(Object), "value");
            var result = Expression.Variable(typeof(Object), "result");
            var state = Expression.Parameter(typeof(IDictionary<String, Object>), "state");
            var source = Expression.Variable(type, "source");

            methodBody.Add(Expression.Assign(source, Expression.Convert(value, type)));

            foreach (var binding in bindings)
            {
                var tryGetValue = Expression.Call(state, DictionaryTryGetValueMethod, Expression.Constant(binding.Metadata.Name), result);
                var assignValue = Expression.Assign(Expression.Field(source, binding.Field), Expression.Convert(result, binding.Field.FieldType));
                var expression = binding.Metadata.IsRequired
                                     ? Expression.IfThenElse(tryGetValue, assignValue, Expression.Throw(Expression.New(MissingMemberExceptionConstructor, new Expression[] { Expression.Constant(type.FullName), Expression.Constant(binding.Metadata.Name) })))
                                     : Expression.IfThen(tryGetValue, assignValue);

                methodBody.Add(expression);
            }

            return Expression.Lambda<Action<Object, IDictionary<String, Object>>>(Expression.Block(new[] { source, result }, methodBody), value, state).Compile();
        }

        /// <summary>
        /// Represents the binding information and metadata for a given <see cref="FieldInfo"/> instance.
        /// </summary>
        private sealed class FieldBinding
        {
            private static readonly Regex BackingFieldPattern = new Regex(@"\<(?<propertyName>[^\>]+)\>k__BackingField", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
            private readonly DataMemberAttribute metadata;
            private readonly PropertyInfo propertyInfo;
            private readonly FieldInfo fieldInfo;
            private readonly Boolean ignore;

            public Boolean Ignore { get { return ignore; } }
            public FieldInfo Field { get { return fieldInfo; } }
            public DataMemberAttribute Metadata { get { return metadata; } }

            /// <summary>
            /// Initializes a new instance of <see cref="FieldBinding"/>.
            /// </summary>
            /// <param name="fieldInfo">The <see cref="FieldInfo"/> associated with this <see cref="FieldBinding"/>.</param>
            public FieldBinding(FieldInfo fieldInfo)
            {
                Verify.NotNull(fieldInfo, "fieldInfo");

                this.fieldInfo = fieldInfo;
                this.propertyInfo = GetPropertyInfo(fieldInfo);
                this.metadata = GetDataMemberAttribute(fieldInfo, propertyInfo);
                this.ignore = CannotBindField(fieldInfo, propertyInfo);
            }

            /// <summary>
            /// Get the associated <see cref="PropertyInfo"/> if the specified <paramref name="fieldInfo"/> represents an auto-property backing field; otherwise returns <value>null</value>.
            /// </summary>
            /// <param name="fieldInfo">The field to attempt locating an associated auto-property.</param>
            private static PropertyInfo GetPropertyInfo(FieldInfo fieldInfo)
            {
                if (fieldInfo.IsInitOnly || fieldInfo.DeclaringType == null)
                    return null;

                var match = BackingFieldPattern.Match(fieldInfo.Name);
                return !match.Success ? null : fieldInfo.DeclaringType.GetProperty(match.Groups["propertyName"].Value, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            /// <summary>
            /// Get the <see cref="DataMemberAttribute"/> associated with the field or property.
            /// </summary>
            /// <param name="fieldInfo">The <see cref="FieldInfo"/> instance to check for a <see cref="DataMemberAttribute"/> instance.</param>
            /// <param name="propertyInfo">The <see cref="PropertyInfo"/> instance to check for a <see cref="DataMemberAttribute"/> instance.</param>
            private static DataMemberAttribute GetDataMemberAttribute(FieldInfo fieldInfo, PropertyInfo propertyInfo)
            {
                DataMemberAttribute attribute;
                String name = propertyInfo == null ? fieldInfo.Name : propertyInfo.Name;

                attribute = propertyInfo == null ? fieldInfo.GetCustomAttribute<DataMemberAttribute>() : propertyInfo.GetCustomAttribute<DataMemberAttribute>();

                if (attribute == null)
                    attribute = new DataMemberAttribute { EmitDefaultValue = false, IsRequired = false, Name = name };

                if (attribute.Name.IsNullOrWhiteSpace())
                    attribute.Name = name;

                return attribute;
            }

            /// <summary>
            /// Returns <value>true</value> if the field should be mapped; otherwise <value>false</value>.
            /// </summary>
            /// <param name="fieldInfo">The <see cref="FieldInfo"/> instance to check for a <see cref="IgnoreDataMemberAttribute"/> instance.</param>
            /// <param name="propertyInfo">The <see cref="PropertyInfo"/> instance to check for a <see cref="IgnoreDataMemberAttribute"/> instance.</param>
            private static Boolean CannotBindField(FieldInfo fieldInfo, PropertyInfo propertyInfo)
            {
                return fieldInfo.IsInitOnly ||
                       fieldInfo.GetCustomAttribute<NonSerializedAttribute>() != null ||
                       fieldInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() != null ||
                       propertyInfo != null && propertyInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() != null;
            }
        }

        /// <summary>
        /// Represents an object binding for a given object type.
        /// </summary>
        private sealed class ObjectBinding
        {
            private readonly IReadOnlyDictionary<String, FieldBinding> fields;
            private readonly Func<Object, IDictionary<String, Object>> getState;
            private readonly Action<Object, IDictionary<String, Object>> setState;

            /// <summary>
            /// Initializes a new instance of <see cref="ObjectBinding"/>.
            /// </summary>
            /// <param name="fields">The object fields.</param>
            /// <param name="getState">The read binding.</param>
            /// <param name="setState">The write binding.</param>
            public ObjectBinding(FieldBinding[] fields, Func<Object, IDictionary<String, Object>> getState, Action<Object, IDictionary<String, Object>> setState)
            {
                Verify.NotNull(fields, "fields");
                Verify.NotNull(getState, "getState");
                Verify.NotNull(setState, "setState");

                this.fields = fields.ToDictionary(binding => binding.Metadata.Name, binding => binding);
                this.getState = getState;
                this.setState = setState;
            }

            /// <summary>
            /// Get the field type for the specified attribute <paramref name="name"/>.
            /// </summary>
            /// <param name="name">The name of the field.</param>
            public Type GetFieldType(String name)
            {
                FieldBinding binding;

                return fields.TryGetValue(name, out binding) ? binding.Field.FieldType : typeof(Object);
            }

            /// <summary>
            /// Get the underlying <see cref="Object"/> mutable state information.
            /// </summary>
            /// <param name="value">The object graph for which fields/properties are to be mapped.</param>
            public IDictionary<String, Object> GetState(Object value)
            {
                return getState(value);
            }

            /// <summary>
            /// Set the underlying <see cref="Object"/> mutable state information.
            /// </summary>
            /// <param name="value">The object graph for which fields/properties are to be mapped.</param>
            /// <param name="state">The source state to be mapped on to <paramref name="value"/>.</param>
            public void SetState(Object value, IDictionary<String, Object> state)
            {
                setState(value, state);
            }
        }
    }
}
