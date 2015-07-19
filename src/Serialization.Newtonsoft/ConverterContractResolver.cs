using System;
using System.Collections.Generic;
using System.Linq;
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
    /// An <see cref="IContractResolver"/> implementation that assigns a default <see cref="JsonConverter"/> for a given <see cref="JsonContract"/>.
    /// </summary>
    /// <remarks>
    /// Optimized to ensure that the set of <see cref="JsonConverter"/> instances are not enumerated on each serialize/deserialize call.
    /// </remarks>
    public sealed class ConverterContractResolver : DefaultContractResolver
    {
        private readonly IReadOnlyList<JsonConverter> knownConverters;

        /// <summary>
        /// Initializes a new instance of <see cref="ConverterContractResolver"/> using the set of specified <see cref="JsonConverter"/> objects.
        /// </summary>
        /// <param name="jsonConverters">The optional set of custom <see cref="JsonConverter"/> instances.</param>
        public ConverterContractResolver(IEnumerable<JsonConverter> jsonConverters)
        {
            knownConverters = GetKnownConverters(jsonConverters);
        }

        /// <summary>
        /// Gets the merged set of required and custom <see cref="JsonConverter"/> objects.
        /// </summary>
        /// <param name="jsonConverters">The optional set of custom <see cref="JsonConverter"/> objects.</param>
        internal static IReadOnlyList<JsonConverter> GetKnownConverters(IEnumerable<JsonConverter> jsonConverters)
        {
            return typeof(ConverterContractResolver).Assembly
                                                    .GetTypes()
                                                    .Where(type => !type.IsAbstract && type.IsClass && type.DerivesFrom(typeof(JsonConverter)))
                                                    .Select(type => (JsonConverter)Activator.CreateInstance(type)).Concat(jsonConverters.EmptyIfNull())
                                                    .OrderByDescending(converter => converter.CanRead && converter.CanWrite)
                                                    .ThenBy(converter => converter.GetType().FullName)
                                                    .ToList();
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
    }
}
