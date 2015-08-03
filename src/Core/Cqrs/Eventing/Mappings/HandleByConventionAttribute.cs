using System;
using System.Reflection;

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

namespace Spark.Cqrs.Eventing.Mappings
{
    /// <summary>
    /// Indicates that event handler methods are mapped by convention (Default).
    /// </summary>
    /// <remarks>
    /// Methods must by of the form:
    /// <code>
    /// [public|protected] void Handle(Event e[, Object service, ...]);
    /// </code>
    /// unless <see cref="MethodName"/>overriden.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class HandleByConventionAttribute : HandleByReflectionAttribute
    {
        /// <summary>
        /// Gets or sets the case-insensitive handle method name (Default is <value>Handle</value>).
        /// </summary>
        public String MethodName { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="HandleByConventionAttribute"/>.
        /// </summary>
        public HandleByConventionAttribute()
        {
            MethodName = "Handle";
        }
        
        /// <summary>
        /// Determine if the specified <paramref name="method"/> conforms to the configured handle method specification.
        /// </summary>
        /// <param name="method">The method info for the handle method candidate.</param>
        protected override Boolean MatchesHandleMethodDefinition(MethodInfo method)
        {
            var parameters = method.GetParameters();

            return method.ReturnParameter != null &&
                   method.ReturnParameter.ParameterType == typeof(void) &&
                   method.Name.Equals(MethodName, StringComparison.InvariantCultureIgnoreCase) &&
                   parameters.Length > 0 && parameters[0].ParameterType.DerivesFrom(typeof(Event));
        }
    }
}
