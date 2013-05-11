using System;
using System.Collections.Generic;
using System.Linq;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.EventStore;
using Spark.Infrastructure.Logging;

/* Copyright (c) 2012 Spark Software Ltd.
 * 
 * This source is subject to the GNU Lesser General Public License.
 * See: http://www.gnu.org/copyleft/lesser.html
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
 * IN THE SOFTWARE. 
 */

namespace Spark.Infrastructure.Domain
{
    /// <summary>
    /// A pluggable <see cref="IStoreAggregates"/> wrapper that enables one or more <see cref="PipelineHook"/> implementations to extend <see cref="Get"/> and/or <see cref="Save"/> behavior.
    /// </summary>
    /// <remarks>
    /// When used in conjunction with <see cref="CachedAggregateStore"/> ensure that <see cref="HookableAggregateStore"/> decorates <see cref="CachedAggregateStore"/> otherwise <see cref="Get"/>
    /// pipeline hooks may not be invoked if the <see cref="Aggregate"/> was in the cache.
    /// </remarks>
    public sealed class HookableAggregateStore : IStoreAggregates
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private readonly IStoreAggregates aggregateStore;
        private readonly PipelineHook[] postSaveHooks;
        private readonly PipelineHook[] preSaveHooks;
        private readonly PipelineHook[] postGetHooks;
        private readonly PipelineHook[] preGetHooks;

        /// <summary>
        /// Get the ordered set of post-save pipeline hooks.
        /// </summary>
        internal IEnumerable<PipelineHook> PostSaveHooks { get { return postSaveHooks; }}

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
        internal IEnumerable<PipelineHook> PreGetHooks{ get { return preGetHooks; }}

        /// <summary>
        /// Initializes a new instance of <see cref="HookableAggregateStore"/>.
        /// </summary>
        /// <param name="aggregateStore">The underlying <see cref="IStoreAggregates"/> implementation to be decorated.</param>
        /// <param name="pipelineHooks">The set of zero or more <see cref="PipelineHook"/> implementations used to extend <see cref="IStoreAggregates"/> behavior.</param>
        public HookableAggregateStore(IStoreAggregates aggregateStore, IEnumerable<PipelineHook> pipelineHooks)
            : this(aggregateStore, pipelineHooks.EmptyIfNull().OrderBy(hook => hook.Order).ThenBy(hook => hook.GetType().FullName).ToList())
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="HookableAggregateStore"/> with <paramref name="pipelineHooks"/> safe to enumerate multiple times.
        /// </summary>
        /// <param name="aggregateStore">The underlying <see cref="IStoreAggregates"/> implementation to be decorated.</param>
        /// <param name="pipelineHooks">The set of zero or more <see cref="PipelineHook"/> implementations used to extend <see cref="IStoreAggregates"/> behavior.</param>
        private HookableAggregateStore(IStoreAggregates aggregateStore, IList<PipelineHook> pipelineHooks)
        {
            Verify.NotNull(aggregateStore, "aggregateStore");

            this.aggregateStore = aggregateStore;
            this.preGetHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPreGet).ToArray();
            this.postGetHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPostGet).Reverse().ToArray();
            this.preSaveHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPreSave).ToArray();
            this.postSaveHooks = pipelineHooks.Where(pipelineHook => pipelineHook.ImplementsPostSave).Reverse().ToArray();
        }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="CachedAggregateStore"/> class.
        /// </summary>
        public void Dispose()
        {
            postSaveHooks.DisposeAll();
            preSaveHooks.DisposeAll();
            postGetHooks.DisposeAll();
            preGetHooks.DisposeAll();
            aggregateStore.Dispose();
        }

        /// <summary>
        /// Retrieve the aggregate of the specified <paramref name="aggregateType"/> and aggregate <paramref name="id"/>.
        /// </summary>
        /// <param name="aggregateType">The type of aggregate to retrieve.</param>
        /// <param name="id">The unique aggregate id.</param>
        public Aggregate Get(Type aggregateType, Guid id)
        {
            Aggregate aggregate;

            InvokePreGetHooks(aggregateType, id);

            aggregate = aggregateStore.Get(aggregateType, id);

            InvokePostGetHooks(aggregate);

            return aggregate;
        }

        /// <summary>
        /// Invokes zero or more customized <see cref="PipelineHook.PreGet"/> implementations.
        /// </summary>
        /// <param name="aggregateType">The type of aggregate to retrieve.</param>
        /// <param name="id">The unique aggregate id.</param>
        private void InvokePreGetHooks(Type aggregateType, Guid id)
        {
            foreach (var pipelineHook in preGetHooks)
            {
                Log.TraceFormat("Invoking pre-get pipeline hook: {0}", pipelineHook);
                pipelineHook.PreGet(aggregateType, id);
            }
        }

        /// <summary>
        /// Invokes zero or more customized <see cref="PipelineHook.PostGet"/> implementations.
        /// </summary>
        /// <param name="aggregate">The loaded aggregate instance.</param>
        private void InvokePostGetHooks(Aggregate aggregate)
        {
            foreach (var pipelineHook in postGetHooks)
            {
                Log.TraceFormat("Invoking post-get pipeline hook: {0}", pipelineHook);
                pipelineHook.PostGet(aggregate);
            }
        }

        /// <summary>
        /// Save the specified <paramref name="context"/> changes for the given aggregate.
        /// </summary>
        /// <param name="aggregate">The current aggregate version for which the context applies.</param>
        /// <param name="context">The command context containing the aggregate changes to be applied.</param>
        public SaveResult Save(Aggregate aggregate, CommandContext context)
        {
            InvokePreSaveHooks(aggregate, context);

            try
            {
                var result = aggregateStore.Save(aggregate, context);

                InvokePostSaveHooks(result.Aggregate, result.Commit, null);

                return result;
            }
            catch (Exception ex)
            {
                InvokePostSaveHooks(aggregate, null, ex);

                throw;
            }
        }

        /// <summary>
        /// Invokes zero or more customized <see cref="PipelineHook.PreSave"/> implementations.
        /// </summary>
        /// <param name="aggregate">The aggregate to be modified by the current <paramref name="context"/>.</param>
        /// <param name="context">The current <see cref="CommandContext"/> containing the pending aggregate modifications.</param>
        private void InvokePreSaveHooks(Aggregate aggregate, CommandContext context)
        {
            foreach (var pipelineHook in preSaveHooks)
            {
                Log.TraceFormat("Invoking pre-save pipeline hook: {0}", pipelineHook);
                pipelineHook.PreSave(aggregate, context);
            }
        }

        /// <summary>
        /// Invokes zero or more customized <see cref="PipelineHook.PostSave"/> implementations.
        /// </summary>
        /// <param name="aggregate">The modified <see cref="Aggregate"/> instance if <paramref name="commit"/> is not <value>null</value>; otherwise the original <see cref="Aggregate"/> instance if <paramref name="error"/> is not <value>null</value>.</param>
        /// <param name="commit">The <see cref="Commit"/> generated if the save was successful; otherwise <value>null</value>.</param>
        /// <param name="error">The <see cref="Exception"/> thrown if the save was unsuccessful; otherwise <value>null</value>.</param>
        private void InvokePostSaveHooks(Aggregate aggregate, Commit commit, Exception error)
        {
            foreach (var pipelineHook in postSaveHooks)
            {
                Log.TraceFormat("Invoking post-save pipeline hook: {0}", pipelineHook);
                pipelineHook.PostSave(aggregate, commit, error);
            }
        }
    }
}
