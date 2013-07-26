using System;
using Spark.Cqrs.Commanding;
using Spark.EventStore;

/* Copyright (c) 2013 Spark Software Ltd.
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

namespace Spark.Cqrs.Domain
{
    /// <summary>
    /// Enables <see cref="Aggregate"/> state validation to ensure that an <see cref="Aggregate"/> instance has not been modified outside of a the regular workflow.
    /// </summary>
    /// <remarks>
    /// This <see cref="PipelineHook"/> is useful within a development environment to ensure that an aggregate has not been accidently modified outside of the proper
    /// workflow (i.e., Apply methods). The <see cref="AggregateStateValidator"/> should not be enabled in a production environment.
    /// </remarks>
    public sealed class AggregateStateValidator : PipelineHook
    {
        /// <summary>
        /// Verify that the retrieved <paramref name="aggregate"/> state has not been corrupted before returning to the caller.
        /// </summary>
        /// <param name="aggregate">The loaded aggregate instance.</param>
        public override void PostGet(Aggregate aggregate)
        {
            Verify.NotNull(aggregate, "aggregate");

            aggregate.VerifyHash();
        }

        /// <summary>
        /// Verify that the specified <paramref name="aggregate"/> state has not been corrupted before proceeding with the save.
        /// </summary>
        /// <param name="aggregate">The aggregate to be modified by the current <paramref name="context"/>.</param>
        /// <param name="context">The current <see cref="CommandContext"/> containing the pending aggregate modifications.</param>
        public override void PreSave(Aggregate aggregate, CommandContext context)
        {
            aggregate.VerifyHash();
        }

        /// <summary>
        /// Verify that the specified <paramref name="aggregate"/> state has not been corrupted after a failed save attempt and update the check sum if the save was successful..
        /// </summary>
        /// <param name="aggregate">The modified <see cref="Aggregate"/> instance if <paramref name="commit"/> is not <value>null</value>; otherwise the original <see cref="Aggregate"/> instance if <paramref name="error"/> is not <value>null</value>.</param>
        /// <param name="commit">The <see cref="Commit"/> generated if the save was successful; otherwise <value>null</value>.</param>
        /// <param name="error">The <see cref="Exception"/> thrown if the save was unsuccessful; otherwise <value>null</value>.</param>
        public override void PostSave(Aggregate aggregate, Commit commit, Exception error)
        {
            if (error != null)
                aggregate.VerifyHash();

            if (commit != null)
                aggregate.UpdateHash();
        }
    }
}
