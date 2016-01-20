using System;

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

namespace Spark.Cqrs.Domain.Mappings
{
    /// <summary>
    /// Indicates that aggregate event apply methods are explicitly mapped by specified <see cref="ApplyMethodMapping"/> type.
    /// </summary>
    /// <remarks>Indented for use in medium trust environments while maintaining non-public apply methods.</remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ApplyByRegistrationAttribute : ApplyByStrategyAttribute
    {
        private readonly Type applyMethodMappingType;

        /// <summary>
        /// Initializes a new instance of <see cref="ApplyByRegistrationAttribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="ApplyMethodMapping"/> containing the explicit apply method mapping.</param>
        public ApplyByRegistrationAttribute(Type type)
        {
            Verify.NotNull(type, nameof(type));
            Verify.TypeDerivesFrom(typeof(ApplyMethodMapping), type, nameof(type));

            applyMethodMappingType = type;
        }
        
        /// <summary>
        /// Maps the apply methods on the specified type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type on which to locate apply methods.</param>
        protected override ApplyMethodCollection MapApplyMethodsFor(Type aggregateType)
        {
            var mappingBuilder = (ApplyMethodMapping)Activator.CreateInstance(applyMethodMappingType);
            var applyMethods = mappingBuilder.GetMappings();

            return new ApplyMethodCollection(ApplyOptional, applyMethods);
        }
    }
}
