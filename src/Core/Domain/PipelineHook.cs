using System;
using Spark.Infrastructure.Commanding;
using Spark.Infrastructure.EventStore;

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
    /// A set of customized behaviors that may be plugged in to the <see cref="HookableAggregateStore"/>.
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
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="PipelineHook"/> class.
        /// </summary>
        protected virtual void Dispose(Boolean disposing)
        { }

        /// <summary>
        /// When overriden, defines the custom behavior to be invoked prior to retrieving the <paramref name="aggregateType"/> identified by <paramref name="id"/>.
        /// </summary>
        /// <param name="aggregateType">The type of aggregate to retrieve.</param>
        /// <param name="id">The unique aggregate id.</param>
        public virtual void PreGet(Type aggregateType, Guid id)
        { }

        /// <summary>
        /// When overriden, defines the custom behavior to be invokes after successfully retrieving the specified <paramref name="aggregate"/>.
        /// </summary>
        /// <param name="aggregate">The loaded aggregate instance.</param>
        public virtual void PostGet(Aggregate aggregate)
        { }

        /// <summary>
        /// When overriden, defines the custom behavior to be invoked prior to saving the specified <paramref name="aggregate"/>.
        /// </summary>
        /// <param name="aggregate">The aggregate to be modified by the current <paramref name="context"/>.</param>
        /// <param name="context">The current <see cref="CommandContext"/> containing the pending aggregate modifications.</param>
        public virtual void PreSave(Aggregate aggregate, CommandContext context)
        { }

        /// <summary>
        ///  When overriden, defines the custom behavior to be invoked after attempting to save the specified <paramref name="aggregate"/>.
        /// </summary>
        /// <param name="aggregate">The modified <see cref="Aggregate"/> instance if <paramref name="commit"/> is not <value>null</value>; otherwise the original <see cref="Aggregate"/> instance if <paramref name="error"/> is not <value>null</value>.</param>
        /// <param name="commit">The <see cref="Commit"/> generated if the save was successful; otherwise <value>null</value>.</param>
        /// <param name="error">The <see cref="Exception"/> thrown if the save was unsuccessful; otherwise <value>null</value>.</param>
        public virtual void PostSave(Aggregate aggregate, Commit commit, Exception error)
        { }
    }
}
