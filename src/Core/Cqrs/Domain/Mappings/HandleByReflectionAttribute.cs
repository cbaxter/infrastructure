using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spark.Cqrs.Commanding;
using Spark.Resources;

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
    /// Base attribute to support aggregate handle methods mapped by reflection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public abstract class HandleByReflectionAttribute : HandleByStrategyAttribute
    {
        /// <summary>
        /// Gets or sets whether non-public handle methods will be included in handle method search.
        /// </summary>
        public Boolean PublicOnly { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="HandleByReflectionAttribute"/>.
        /// </summary>
        protected HandleByReflectionAttribute()
        {
            PublicOnly = false;
        }

        /// <summary>
        /// Maps the handle methods on the specified type.
        /// </summary>
        /// <param name="aggregateType">The aggregate type on which to locate handle methods.</param>
        /// <param name="serviceProvider">The underlying service provider (IoC Container).</param>
        protected override HandleMethodCollection MapHandleMethodsFor(Type aggregateType, IServiceProvider serviceProvider)
        {
            var bindingFlags = GetBindingFlags(PublicOnly);
            var handleMethods = aggregateType.GetMethods(bindingFlags).Where(MatchesHandleMethodDefinition);
            var mappings = new Dictionary<Type, Action<Aggregate, Command>>();

            foreach (var handleMethod in handleMethods)
            {
                var commandType = handleMethod.GetParameters().First().ParameterType;
                var compiledAction = CompileAction(handleMethod, commandType, serviceProvider);

                if (mappings.ContainsKey(commandType))
                    throw new MappingException(Exceptions.HandleMethodOverloaded.FormatWith(aggregateType, handleMethod));

                mappings.Add(commandType, compiledAction);
            }

            return new HandleMethodCollection(mappings);
        }

        /// <summary>
        /// Gets the reflection binding flags to be used when locating handle methods.
        /// </summary>
        /// <param name="publicOnly">True to exclude <see cref="BindingFlags.NonPublic"/>; otherwise false.</param>
        private static BindingFlags GetBindingFlags(Boolean publicOnly)
        {
            var bindingFlags = BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase | BindingFlags.Public;

            if (!publicOnly)
                bindingFlags |= BindingFlags.NonPublic;

            return bindingFlags;
        }

        /// <summary>
        /// Determine if the specified <paramref name="method"/> conforms to the configured handle method specification.
        /// </summary>
        /// <param name="method">The method info for the apply method candidate.</param>
        protected abstract Boolean MatchesHandleMethodDefinition(MethodInfo method);
    }
}
