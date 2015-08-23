using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    /// An immutable <see cref="ValueType"/> wrapper to facililate strong type usage over primitive obsession.
    /// </summary>
    public abstract class ValueObject
    {
        private static readonly IDictionary<Type, ValueObject> Parsers = new ConcurrentDictionary<Type, ValueObject>();
        internal static readonly Type StringType = typeof(String);

        /// <summary>
        /// Get the underlying boxed value.
        /// </summary>
        internal abstract Object BoxedValue { get; }

        /// <summary>
        /// Intializes a new instance of <see cref="ValueObject"/>.
        /// </summary>
        internal ValueObject()
        { }

        /// <summary>
        /// Parse the <see cref="String"/> <paramref name="value"/> in to an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of value object to parse from the <see cref="String"/> <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to parse.</param>
        public static T Parse<T>(String value)
           where T : ValueObject
        {
            Verify.NotNull(value, nameof(value));

            var result = default(ValueObject);
            if (TryParseInternal(typeof(T), value, out result))
                return (T)result;

            throw new FormatException();
        }

        /// <summary>
        /// Parse the <see cref="String"/> <paramref name="value"/> in to an instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of value object to parse from the <see cref="String"/> <paramref name="value"/>.</param>
        /// <param name="value">The value to parse.</param>
        public static ValueObject Parse(Type type, String value)
        {
            Verify.NotNull(type, nameof(type));
            Verify.NotNull(value, nameof(value));
            Verify.TypeDerivesFrom(typeof(ValueObject), type, nameof(type));

            ValueObject result;
            if (TryParseInternal(type, value, out result))
                return result;

            throw new FormatException();
        }

        /// <summary>
        /// Attempt to parse the <see cref="String"/> <paramref name="value"/> in to an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of value object to parse from the <see cref="String"/> <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to parse.</param>
        /// <param name="result">The parsed instance of <typeparamref name="T"/>.</param>
        public static Boolean TryParse<T>(String value, out T result)
            where T : ValueObject
        {
            Verify.NotNull(value, nameof(value));

            ValueObject valueObject;
            result = TryParseInternal(typeof(T), value, out valueObject) ? (T)valueObject : default(T);

            return result != null;
        }

        /// <summary>
        /// Attempt to parse the <see cref="String"/> <paramref name="value"/> in to an instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of value object to parse from the <see cref="String"/> <paramref name="value"/>.</param>
        /// <param name="value">The value to parse.</param>
        /// <param name="result">The parsed instance of <paramref name="type"/>.</param>
        public static Boolean TryParse(Type type, String value, out ValueObject result)
        {
            Verify.NotNull(type, nameof(type));
            Verify.NotNull(value, nameof(value));
            Verify.TypeDerivesFrom(typeof(ValueObject), type, nameof(type));

            return TryParseInternal(type, value, out result);
        }

        /// <summary>
        /// Attempt to parse the <see cref="String"/> <paramref name="value"/> in to an instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of value object to parse from the <see cref="String"/> <paramref name="value"/>.</param>
        /// <param name="value">The value to parse.</param>
        /// <param name="result">The parsed instance of <paramref name="type"/>.</param>
        private static Boolean TryParseInternal(Type type, String value, out ValueObject result)
        {
            ValueObject parser;

            if (!Parsers.TryGetValue(type, out parser))
                Parsers[type] = parser = (ValueObject)FormatterServices.GetUninitializedObject(type);

            return parser.TryParse(value, out result);
        }

        /// <summary>
        /// Attempt to parse the <see cref="String"/> <paramref name="value"/> in to an instance of the underlying run-time type.
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <param name="result">The parsed instance of the underlying run-time type.</param>
        protected abstract Boolean TryParse(String value, out ValueObject result);
    }

    /// <summary>
    /// An immutable strongly typed <see cref="ValueType"/> wrapper to facililate strong type usage over primitive obsession.
    /// </summary>
    /// <typeparam name="T">The underlying value type represented by this value object instance.</typeparam>
    public abstract class ValueObject<T> : ValueObject, IComparable, IComparable<T>, IComparable<ValueObject<T>>, IEquatable<T>, IEquatable<ValueObject<T>>
        where T : IComparable, IComparable<T>, IEquatable<T>
    {
        private static readonly MemberInfo[] Fields = { typeof(ValueObject<T>).GetField("value", BindingFlags.NonPublic | BindingFlags.Instance) };
        private static readonly Type ValueType = typeof(T);
        private readonly T value;

        /// <summary>
        /// Get the underlying raw value.
        /// </summary>
        protected T RawValue { get { return value; } }

        /// <summary>
        /// Get the underlying boxed value.
        /// </summary>
        internal sealed override Object BoxedValue { get { return value; } }

        /// <summary>
        /// Initializes a new instance of <see cref="ValueObject{T}"/>.
        /// </summary>
        /// <param name="value">The raw value wrapped by this value object instance.</param>
        protected ValueObject(T value)
        {
            if (ReferenceEquals(value, null)) throw new ArgumentNullException(nameof(value));

            this.value = GetValue(value);
        }

        /// <summary>
        /// Get the formatted value used to back this value object instance.
        /// </summary>
        /// <param name="value">The raw value wrapped by this value object instance.</param>
        private T GetValue(T value)
        {
            T result;

            if (TryGetValue(value, out result))
                return result;

            throw new FormatException();
        }

        /// <summary>
        /// Attempt to get the formatted value used to back this value object instance.
        /// </summary>
        /// <param name="value">The raw value wrapped by this value object instance.</param>
        /// <param name="result">The formatted value wrapped by this value object instance.</param>
        protected virtual Boolean TryGetValue(T value, out T result)
        {
            result = value;
            return true;
        }

        /// <summary>
        /// Attempt to parse the <see cref="String"/> <paramref name="value"/> in to an instance of the underlying run-time type.
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <param name="result">The parsed instance of the underlying run-time type.</param>
        protected override sealed Boolean TryParse(String value, out ValueObject result)
        {
            T rawValue;

            if (TryParse(value, out rawValue))
            {
                T formattedValue;

                result = TryGetValue(rawValue, out formattedValue) && !ReferenceEquals(formattedValue, null) ? CreateInstance(GetType(), formattedValue) : null;
            }
            else
            {
                result = null;
            }

            return result != null;
        }

        /// <summary>
        /// Attempt to parse the <see cref="String"/> <paramref name="value"/> in to the base value type to be wrapped by a value object instance.
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <param name="result">The parsed instance of the underlying value type.</param>
        protected virtual Boolean TryParse(String value, out T result)
        {
            if (ValueType != StringType)
                throw new NotSupportedException();

            result = (T)(Object)value;
            return true;
        }

        /// <summary>
        /// Create a new instance of the specified <paramref name="type"/> based on the provided <paramref name="value"/>.
        /// </summary>
        /// <param name="type">The type of value object to create.</param>
        /// <param name="value">The underlying value object value.</param>
        private static ValueObject CreateInstance(Type type, T value)
        {
            var result = (ValueObject<T>)FormatterServices.GetUninitializedObject(type);

            FormatterServices.PopulateObjectMembers(result, Fields, new Object[] { value });

            return result;
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="Object"/> are equal.
        /// </summary>
        /// <param name="other">Another object to compare.</param>
        public sealed override Boolean Equals(Object other)
        {
            return other is T ? Equals((T)other) : Equals(other as ValueObject<T>);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <see cref="ValueObject{T}"/> are equal.
        /// </summary>
        /// <param name="other">Another <see cref="ValueObject{T}"/> to compare.</param>
        public Boolean Equals(ValueObject<T> other)
        {
            return other != null && Equals(other.value);
        }

        /// <summary>
        /// Indicates whether this instance and a specified <typeparamref name="T"/> are equal.
        /// </summary>
        /// <param name="other">Another <typeparamref name="T"/> to compare.</param>
        public virtual Boolean Equals(T other)
        {
            return value.Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {
                var hash = 43;

                hash = (hash * 397) + GetType().GetHashCode();
                hash = (hash * 397) + RawValue.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="other">An object to compare with this instance. </param>
        public Int32 CompareTo(Object other)
        {
            return other is T ? CompareTo((T)other) : CompareTo(other as ValueObject<T>);
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        public virtual Int32 CompareTo(T other)
        {
            return value.CompareTo(other);
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        public Int32 CompareTo(ValueObject<T> other)
        {
            return other == null ? 1 : CompareTo(other.value);
        }

        /// <summary>
        /// Returns a string that represents the current value object instance.
        /// </summary>
        public override String ToString()
        {
            return value.ToString();
        }

        /// <summary>
        /// Implicitly convert this specified <paramref name="value"/> in to an instance of the underlying type (unwrap).
        /// </summary>
        /// <param name="value">The value object to implicitly convert to the underlying type.</param>
        public static implicit operator T(ValueObject<T> value)
        {
            return value == null ? default(T) : value.value;
        }
    }
}
