using System;
using System.Collections.Generic;
using System.Threading;
using Spark.Cqrs.Domain;
using Spark.Cqrs.Eventing;
using Spark.Messaging;
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

namespace Spark.Cqrs.Commanding
{
    /// <summary>
    /// The command context wrapper used when invoking a command on an aggregate.
    /// </summary>
    public sealed class CommandContext : IDisposable
    {
        [ThreadStatic]
        private static CommandContext currentContext;
        private readonly CommandContext originalContext;
        private readonly IList<Event> raisedEvents;
        private readonly HeaderCollection headers;
        private readonly CommandEnvelope envelope;
        private readonly Guid commandId;
        private readonly Thread thread;
        private Boolean disposed;

        /// <summary>
        /// The current <see cref="CommandContext"/> if exists or null if no command context.
        /// </summary>
        public static CommandContext Current { get { return currentContext; } }

        /// <summary>
        /// The message header collection associated with this <see cref="CommandContext"/>.
        /// </summary>
        public HeaderCollection Headers { get { return headers; } }

        /// <summary>
        /// The unique <see cref="Aggregate"/> id associated with this <see cref="CommandContext"/>.
        /// </summary>
        public Guid AggregateId { get { return envelope.AggregateId; } }

        /// <summary>
        /// The unique <see cref="Command"/> message id associated with this <see cref="CommandContext"/>.
        /// </summary>
        public Guid CommandId { get { return commandId; } }

        /// <summary>
        /// The <see cref="Command"/> associated with this <see cref="CommandContext"/>.
        /// </summary>
        public Command Command { get { return envelope.Command; } }

        /// <summary>
        /// Return <value>True</value> if one or more events were raised when handling the underlying <see cref="Command"/>; otherwise <value>False</value>.
        /// </summary>
        internal Boolean HasRaisedEvents { get { return raisedEvents.Count > 0; } }

        /// <summary>
        /// Initializes a new instance of <see cref="CommandContext"/> with the specified <paramref name="commandId"/> and <paramref name="headers"/>.
        /// </summary>
        /// <param name="commandId">The unique <see cref="Command"/> identifier.</param>
        /// <param name="headers">The <see cref="Command"/> headers.</param>
        /// <param name="envelope">The <see cref="CommandEnvelope"/> associated with this context.</param>
        public CommandContext(Guid commandId, HeaderCollection headers, CommandEnvelope envelope)
        {
            Verify.NotNull(headers, nameof(headers));
            Verify.NotNull(envelope, nameof(envelope));

            this.raisedEvents = new List<Event>();
            this.originalContext = currentContext;
            this.thread = Thread.CurrentThread;
            this.commandId = commandId;
            this.envelope = envelope;
            this.headers = headers;

            currentContext = this;
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="CommandContext"/> class.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            if (thread != Thread.CurrentThread)
                throw new InvalidOperationException(Exceptions.CommandContextInterleaved);

            if (this != Current)
                throw new InvalidOperationException(Exceptions.CommandContextInvalidThread);

            disposed = true;
            currentContext = originalContext;
        }

        /// <summary>
        /// Gets the current <see cref="CommandContext"/> if exists or throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        public static CommandContext GetCurrent()
        {
            if (currentContext == null)
                throw new InvalidOperationException(Exceptions.NoCommandContext);

            return currentContext;
        }

        /// <summary>
        /// Add the specified <see cref="Event"/> <paramref name="e"/> to the current <see cref="CommandContext"/>.
        /// </summary>
        /// <param name="e">The <see cref="Event"/> to be raise.</param>
        internal void Raise(Event e)
        {
            Verify.NotNull(e, nameof(e));
            Verify.NotDisposed(this, disposed);

            raisedEvents.Add(e);
        }

        /// <summary>
        /// Gets the set of <see cref="Event"/> instances raised within the current <see cref="CommandContext"/>.
        /// </summary>
        /// <returns></returns>
        public EventCollection GetRaisedEvents()
        {
            Verify.NotDisposed(this, disposed);

            return new EventCollection(raisedEvents);
        }

        /// <summary>
        /// Returns the <see cref="CommandContext"/> description for this instance.
        /// </summary>
        public override String ToString()
        {
            return $"{CommandId} - {Command}";
        }
    }
}
