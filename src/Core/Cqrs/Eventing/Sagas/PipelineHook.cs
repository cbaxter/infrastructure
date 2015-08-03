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

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// A set of customized behaviors that may be plugged in to the <see cref="HookableSagaStore"/>.
    /// </summary>
    public abstract class PipelineHook : IDisposable
    {
        private static readonly Type PipelineHookType = typeof(PipelineHook);

        /// <summary>
        /// Return true if <see cref="PostSave"/> has been explicitly overriden; otherwise false.
        /// </summary>
        internal Boolean ImplementsPostSave { get; private set; }

        /// <summary>
        /// Return true if <see cref="PreSave"/> has been explicitly overriden; otherwise false.
        /// </summary>
        internal Boolean ImplementsPreSave { get; private set; }

        /// <summary>
        /// Return true if <see cref="PostGet"/> has been explicitly overriden; otherwise false.
        /// </summary>
        internal Boolean ImplementsPostGet { get; private set; }

        /// <summary>
        /// Return true if <see cref="PreGet"/> has been explicitly overriden; otherwise false.
        /// </summary>
        internal Boolean ImplementsPreGet { get; private set; }

        /// <summary>
        /// The ordinal value that specifies an explicit invoke order for this <see cref="PipelineHook"/> instance.
        /// </summary>
        internal Int32 Order { get; private set; }

        /// <summary>
        /// Initializes a new instance of a <see cref="PipelineHook"/>.
        /// </summary>
        protected PipelineHook()
            : this(0)
        { }

        /// <summary>
        /// Initializes a new instance of a <see cref="PipelineHook"/> using the specified <paramref name="ordinal"/>.
        /// </summary>
        /// <remarks>
        /// The underlying <see cref="Type.FullName"/> will be used as a secondary sort if the same ordinal value is assigned to more than one <see cref="PipelineHook"/>.
        /// </remarks>
        /// <param name="ordinal">The ordinal value that specifies an explicit invoke order for this <see cref="PipelineHook"/> instance</param>
        protected PipelineHook(Int32 ordinal)
        {
            var type = GetType();

            Order = ordinal;
            ImplementsPreGet = type.GetMethod("PreGet").DeclaringType != PipelineHookType;
            ImplementsPostGet = type.GetMethod("PostGet").DeclaringType != PipelineHookType;
            ImplementsPreSave = type.GetMethod("PreSave").DeclaringType != PipelineHookType;
            ImplementsPostSave = type.GetMethod("PostSave").DeclaringType != PipelineHookType;
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="PipelineHook"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="PipelineHook"/> class.
        /// </summary>
        protected virtual void Dispose(Boolean disposing)
        { }

        /// <summary>
        /// When overriden, defines the custom behavior to be invoked prior to retrieving the <paramref name="sagaType"/> identified by <paramref name="sagaId"/>.
        /// </summary>
        /// <param name="sagaType">The type of saga to retrieve.</param>
        /// <param name="sagaId">The saga correlation id.</param>
        public virtual void PreGet(Type sagaType, Guid sagaId)
        { }

        /// <summary>
        /// When overriden, defines the custom behavior to be invokes after successfully retrieving the specified <paramref name="saga"/>.
        /// </summary>
        /// <param name="saga">The loaded saga instance.</param>
        public virtual void PostGet(Saga saga)
        { }

        /// <summary>
        /// When overriden, defines the custom behavior to be invoked prior to saving the specified <paramref name="saga"/>.
        /// </summary>
        /// <param name="saga">The saga to be modified using the current <paramref name="context"/>.</param>
        /// <param name="context">The current <see cref="SagaContext"/> assocaited with the pending saga modifications.</param>
        public virtual void PreSave(Saga saga, SagaContext context)
        { }

        /// <summary>
        ///  When overriden, defines the custom behavior to be invoked after attempting to save the specified <paramref name="saga"/>.
        /// </summary>
        /// <param name="saga">The modified <see cref="Saga"/> instance if <paramref name="error"/> is <value>null</value>; otherwise the original <see cref="Saga"/> instance if <paramref name="error"/> is not <value>null</value>.</param>
        /// <param name="context">The current <see cref="SagaContext"/> assocaited with the pending saga modifications.</param>
        /// <param name="error">The <see cref="Exception"/> thrown if the save was unsuccessful; otherwise <value>null</value>.</param>
        public virtual void PostSave(Saga saga, SagaContext context, Exception error)
        { }
    }
}
