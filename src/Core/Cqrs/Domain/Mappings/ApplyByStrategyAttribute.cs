using System;
using System.Linq.Expressions;
using System.Reflection;
using Spark.Cqrs.Eventing;

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
    /// Base attribute for indicating apply method mapping strategy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public abstract class ApplyByStrategyAttribute : Attribute
    {
        /// <summary>
        /// Represents the default <see cref="ApplyByStrategyAttribute"/> instance. This field is read-only.
        /// </summary>
        public static readonly ApplyByStrategyAttribute Default = new ApplyByConventionAttribute();

        /// <summary>
        /// Gets or sets whether or not an event apply method is optional (Default is <value>true</value>).
        /// </summary>
        public Boolean ApplyOptional { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ApplyByStrategyAttribute"/>.
        /// </summary>
        protected ApplyByStrategyAttribute()
        {
            ApplyOptional = true;
        }

        /// <summary>
        /// Gets the apply method collection associated with the given aggregate type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        internal ApplyMethodCollection GetApplyMethods(Type aggregateType)
        {
            Verify.NotNull(aggregateType, nameof(aggregateType));
            Verify.TypeDerivesFrom(typeof(Aggregate), aggregateType, nameof(aggregateType));

            return MapApplyMethodsFor(aggregateType);
        }

        /// <summary>
        /// Creates the apply method collection for a given aggregate type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type.</param>
        protected abstract ApplyMethodCollection MapApplyMethodsFor(Type aggregateType);

        /// <summary>
        /// Compiles an apply method in to an invokable action.
        /// </summary>
        /// <param name="applyMethod">The reflected apply method.</param>
        /// <param name="eventType">The event type handled.</param>
        protected Action<Aggregate, Event> CompileAction(MethodInfo applyMethod, Type eventType)
        {
            var eventParameter = Expression.Parameter(typeof(Event), "e");
            var aggregateParameter = Expression.Parameter(typeof(Aggregate), "aggregate");
            var arguments = new Expression[] { Expression.Convert(eventParameter, eventType) };
            var body = Expression.Call(Expression.Convert(aggregateParameter, applyMethod.ReflectedType), applyMethod, arguments);
            var expression = Expression.Lambda<Action<Aggregate, Event>>(body, aggregateParameter, eventParameter);

            return expression.Compile();
        }
    }
}
