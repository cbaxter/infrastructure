using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

namespace Spark.Serialization
{
    /// <summary>
    /// An <see cref="IContractResolver"/> implementation that will use the <see cref="DataMemberAttribute.Name"/> when available over the 
    /// default <see cref="JsonProperty.PropertyName"/>. This <see cref="IContractResolver"/> will always use private property setters over
    /// explicit constructor parameters when initializing an object during deserialization.
    /// </summary>
    /// <remarks>
    /// Optimized to ensure that the set of <see cref="JsonConverter"/> instances are not enumerated on each serialize/deserialize call.
    /// </remarks>
    public sealed class DataMemberContractResolver : DefaultContractResolver
    {
        private static readonly Boolean CanUseNonPublicSetters = AppDomain.CurrentDomain.IsFullyTrusted;
        private readonly IReadOnlyList<JsonConverter> knownConverters;

        /// <summary>
        /// Initializes a new instance of <see cref="ConverterContractResolver"/> using the set of specified <see cref="JsonConverter"/> objects.
        /// </summary>
        /// <param name="jsonConverters"></param>
        public DataMemberContractResolver(IEnumerable<JsonConverter> jsonConverters)
        {
            knownConverters = ConverterContractResolver.GetKnownConverters(jsonConverters);
        }

        /// <summary>
        /// Determine which contract type is created for the given <paramref name="objectType"/>.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);

            // Assigns the first JsonConverter that can convert the specified object type favoring converts that support Read and Write over just Read or Write.
            contract.Converter = knownConverters.FirstOrDefault(converter => converter.CanConvert(objectType));

            return contract;
        }

        /// <summary>
        /// Determines which contract type is created for the given type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);

            if (contract.DefaultCreator == null)
                contract.DefaultCreator = () => FormatterServices.GetUninitializedObject(objectType);

            return contract;
        }

        /// <summary>
        /// Creates a <see cref="JsonProperty"/> for the given class <paramref name="member"/>.
        /// </summary>
        /// <param name="memberSerialization">The member's parent <see cref="MemberSerialization"/>.</param>
        /// <param name="member">The member to create a <see cref="JsonProperty"/> for.</param>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var jsonProperty = base.CreateProperty(member, memberSerialization);
            var property = jsonProperty.Writable ? null : member as PropertyInfo;
            var dataMember = member.GetCustomAttributes().OfType<DataMemberAttribute>().SingleOrDefault();

            if (property != null)
                jsonProperty.Writable = property.GetSetMethod(nonPublic: CanUseNonPublicSetters) != null;

            if (dataMember != null && dataMember.Name.IsNotNullOrWhiteSpace() && jsonProperty.PropertyName.Equals(jsonProperty.UnderlyingName))
                jsonProperty.PropertyName = dataMember.Name;

            return jsonProperty;
        }
    }
}
