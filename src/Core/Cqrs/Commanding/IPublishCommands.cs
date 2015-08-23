using System;
using System.Collections.Generic;
using Spark.Cqrs.Domain;
using Spark.Messaging;

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

namespace Spark.Cqrs.Commanding
{
    /// <summary>
    /// Command publisher.
    /// </summary>
    public interface IPublishCommands
    {
        /// <summary>
        /// Publishes the specified <paramref name="payload"/> on the underlying message bus.
        /// </summary>
        /// <param name="headers">The set of message headers associated with the command.</param>
        /// <param name="payload">The command payload to be published.</param>
        void Publish(IEnumerable<Header> headers, CommandEnvelope payload);
    }

    /// <summary>
    /// Extension methods of <see cref="IPublishCommands"/>
    /// </summary>
    public static class CommandPublisherExtensions
    {
        /// <summary>
        /// Publishes the set of <paramref name="commands"/> with only the default message headers.
        /// </summary>
        /// <param name="publisher">The command publisher.</param>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="commands"/>.</param>
        /// <param name="commands">The commands to be published.</param>
        public static void Publish(this IPublishCommands publisher, Guid aggregateId, IEnumerable<Command> commands)
        {
            Verify.NotNull(publisher, nameof(publisher));

            foreach (var command in commands.EmptyIfNull())
                publisher.Publish(aggregateId, command);
        }

        /// <summary>
        /// Publishes the specified <paramref name="command"/> with only the default message headers.
        /// </summary>
        /// <param name="publisher">The command publisher.</param>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="command"/>.</param>
        /// <param name="command">The command to be published.</param>
        public static void Publish(this IPublishCommands publisher, Guid aggregateId, Command command)
        {
            Verify.NotNull(publisher, nameof(publisher));

            publisher.Publish(null, new CommandEnvelope(aggregateId, command));
        }

        /// <summary>
        /// Publishes the specified <paramref name="command"/> with the set of custom message headers.
        /// </summary>
        /// <param name="publisher">The command publisher.</param>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="command"/>.</param>
        /// <param name="command">The command to be published.</param>
        /// <param name="headers">The set of one or more custom message headers.</param>
        public static void Publish(this IPublishCommands publisher, Guid aggregateId, Command command, params Header[] headers)
        {
            Verify.NotNull(publisher, nameof(publisher));

            publisher.Publish(headers, new CommandEnvelope(aggregateId, command));
        }

        /// <summary>
        /// Publishes the specified <paramref name="command"/> with the enumerable set of custom message headers.
        /// </summary>
        /// <param name="publisher">The command publisher.</param>
        /// <param name="aggregateId">The <see cref="Aggregate"/> identifier that will handle the specified <paramref name="command"/>.</param>
        /// <param name="command">The command to be published.</param>
        /// <param name="headers">The set of message headers associated with the command.</param>
        public static void Publish(this IPublishCommands publisher, Guid aggregateId, Command command, IEnumerable<Header> headers)
        {
            Verify.NotNull(publisher, nameof(publisher));

            publisher.Publish(headers, new CommandEnvelope(aggregateId, command));
        }
    }
}
