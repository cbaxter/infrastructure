using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Logging;

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
    /// A pluggable <see cref="IStoreSagas"/> wrapper that enables one or more <see cref="PipelineHook"/> implementations to 
    /// extend <see cref="CreateSaga"/>, <see cref="TryGetSaga"/> and/or <see cref="Save"/> behavior.
    /// </summary>
    /// <remarks>
    /// When used in conjunction with <see cref="CachedSagaStore"/> ensure that <see cref="HookableSagaStore"/> decorates <see cref="CachedSagaStore"/> otherwise load
    /// pipeline hooks may not be invoked if the <see cref="Saga"/> was in the cache.
    /// </remarks>
    public sealed class HookableSagaStore : IStoreSagas
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IStoreSagas sagaStore;
        private readonly PipelineHook[] postSaveHooks;
        private readonly PipelineHook[] preSaveHooks;
        private readonly PipelineHook[] postGetHooks;
        private readonly PipelineHook[] preGetHooks;

        /// <summary>
        /// Get the ordered set of post-save pipeline hooks.
        /// </summary>
        internal IEnumerable<PipelineHook> PostSaveHooks { get { return postSaveHooks; } }

        /// <summary>
        /// Get the ordered set of pre-save pipeline hooks.
        /// </summary>
        internal IEnumerable<PipelineHook> PreSaveHooks { get { return preSaveHooks; } }

        /// <summary>
        /// Get the ordered set of post-get pipeline hooks.
        /// </summary>
        internal IEnumerable<PipelineHook> PostGetHooks { get { return postGetHooks; } }

        /// <summary>
        /// Get the ordered set of pre-get pipeline hooks.
        /// </summary>
        internal IEnumerable<PipelineHook> PreGetHooks { get { return preGetHooks; } }

        /// <summary>
        /// Initializes a new instance of <see cref="HookableSagaStore"/>.
        /// </summary>
        /// <param name="sagaStore">The underlying <see cref="IStoreSagas"/> implementation to be decorated.</param>
        /// <param name="pipelineHooks">The set of zero or more <see cref="PipelineHook"/> implementations used to extend <see cref="IStoreSagas"/> behavior.</param>
        public HookableSagaStore(IStoreSagas sagaStore, IEnumerable<PipelineHook> pipelineHooks)
            : this(sagaStore, pipelineHooks.EmptyIfNull().OrderBy(hook => hook.Order).ThenBy(hook => hook.GetType().FullName).ToList())
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="HookableSagaStore"/> with <paramref name="pipelineHooks"/> safe to enumerate multiple times.
        /// </summary>
        /// <param name="sagaStore">The underlying <see cref="IStoreSagas"/> implementation to be decorated.</param>
        /// <param name="pipelineHooks">The set of zero or more <see cref="PipelineHook"/> implementations used to extend <see cref="IStoreSagas"/> behavior.</param>
        private HookableSagaStore(IStoreSagas sagaStore, IList<PipelineHook> pipelineHooks)
        {
            Verify.NotNull(sagaStore, nameof(sagaStore));

            this.sagaStore = sagaStore;
            this.preGetHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPreGet).ToArray();
            this.postGetHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPostGet).Reverse().ToArray();
            this.preSaveHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPreSave).ToArray();
            this.postSaveHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPostSave).Reverse().ToArray();
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="PipelineHook"/> class.
        /// </summary>
        public void Dispose()
        {
            postSaveHooks.DisposeAll();
            preSaveHooks.DisposeAll();
            postGetHooks.DisposeAll();
            preGetHooks.DisposeAll();
            sagaStore.Dispose();
        }

        /// <summary>
        /// Creates a new saga instance identified by the specified <paramref name="type"/> and <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The type of saga to be retrieved.</param>
        /// <param name="id">The correlation id of the saga to be retrieved.</param>
        public Saga CreateSaga(Type type, Guid id)
        {
            Saga saga;

            InvokePreGetHooks(type, id);

            saga = sagaStore.CreateSaga(type, id);

            InvokePostGetHooks(saga);

            return saga;
        }

        /// <summary>
        /// Attempt to retrieve an existing saga instance identified by the specified <paramref name="type"/> and <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The type of saga to be retrieved.</param>
        /// <param name="id">The correlation id of the saga to be retrieved.</param>
        /// <param name="saga">The <see cref="Saga"/> instance if found; otherwise <value>null</value>.</param>
        public Boolean TryGetSaga(Type type, Guid id, out Saga saga)
        {
            Boolean result;

            InvokePreGetHooks(type, id);

            result = sagaStore.TryGetSaga(type, id, out saga);

            InvokePostGetHooks(saga);

            return result;
        }

        /// <summary>
        /// Get all scheduled saga timeouts before the specified maximum timeout.
        /// </summary>
        /// <param name="maximumTimeout">The exclusive timeout upper bound.</param>
        public IReadOnlyList<SagaTimeout> GetScheduledTimeouts(DateTime maximumTimeout)
        {
            return sagaStore.GetScheduledTimeouts(maximumTimeout);
        }

        /// <summary>
        /// Save the specified <paramref name="context"/> changes for the given <paramref name="saga"/>.
        /// </summary>
        /// <param name="saga">The current saga version for which the context applies.</param>
        /// <param name="context">The saga context containing the saga changes to be applied.</param>
        public Saga Save(Saga saga, SagaContext context)
        {
            InvokePreSaveHooks(saga, context);

            try
            {
                var result = sagaStore.Save(saga, context);

                InvokePostSaveHooks(result, context, null);

                return result;
            }
            catch (Exception ex)
            {
                InvokePostSaveHooks(saga, context, ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes all existing sagas from the saga store.
        /// </summary>
        public void Purge()
        {
            sagaStore.Purge();
        }

        /// <summary>
        /// Invokes zero or more customized <see cref="PipelineHook.PreGet"/> implementations.
        /// </summary>
        /// <param name="type">The type of saga to retrieve.</param>
        /// <param name="id">The saga correlation id.</param>
        private void InvokePreGetHooks(Type type, Guid id)
        {
            foreach (var pipelineHook in preGetHooks)
            {
                Log.TraceFormat("Invoking pre-get pipeline hook: {0}", pipelineHook);
                pipelineHook.PreGet(type, id);
            }
        }

        /// <summary>
        /// Invokes zero or more customized <see cref="PipelineHook.PostGet"/> implementations.
        /// </summary>
        /// <param name="saga">The loaded saga instance.</param>
        private void InvokePostGetHooks(Saga saga)
        {
            foreach (var pipelineHook in postGetHooks)
            {
                Log.TraceFormat("Invoking post-get pipeline hook: {0}", pipelineHook);
                pipelineHook.PostGet(saga);
            }
        }
        /// <summary>
        /// Invokes zero or more customized <see cref="PipelineHook.PreSave"/> implementations.
        /// </summary>
        /// <param name="saga">The saga to be modified by the current <paramref name="context"/>.</param>
        /// <param name="context">The current <see cref="SagaContext"/> associated with the pending saga modifications.</param>
        private void InvokePreSaveHooks(Saga saga, SagaContext context)
        {
            foreach (var pipelineHook in preSaveHooks)
            {
                Log.TraceFormat("Invoking pre-save pipeline hook: {0}", pipelineHook);
                pipelineHook.PreSave(saga, context);
            }
        }

        /// <summary>
        /// Invokes zero or more customized <see cref="PipelineHook.PostSave"/> implementations.
        /// </summary>
        /// <param name="saga">The modified <see cref="Saga"/> instance if <paramref name="error"/> is <value>null</value>; otherwise the original <see cref="Saga"/> instance if <paramref name="error"/> is not <value>null</value>.</param>
        /// <param name="context">The current <see cref="SagaContext"/> assocaited with the pending saga modifications.</param>
        /// <param name="error">The <see cref="Exception"/> thrown if the save was unsuccessful; otherwise <value>null</value>.</param>
        private void InvokePostSaveHooks(Saga saga, SagaContext context, Exception error)
        {
            foreach (var pipelineHook in postSaveHooks)
            {
                Log.TraceFormat("Invoking post-save pipeline hook: {0}", pipelineHook);
                pipelineHook.PostSave(saga, context, error);
            }
        }
    }
}
