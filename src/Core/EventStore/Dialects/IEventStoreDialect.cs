using System;
using System.Data.Common;

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

namespace Spark.Infrastructure.EventStore.Dialects
{
    /// <summary>
    /// The RDBMS dialect statements associated with an <see cref="DbEventStore"/> instance.
    /// </summary>
    internal interface IEventStoreDialect : IDialect
    {
        Int32 PageSize { get; }
        String GetStream { get; }
        String GetCommits { get; }
        String InsertCommitStatement { get; }
        String UpdateCommitStatement { get; }
        String DeleteStreamStatement { get; }
        String DeleteStreamsStatement { get; }
        String EnsureCommitTableCreatedStatement { get; }
        String EnsureDuplicateCommitsSuppressedStatement { get; }
        String EnsureDuplicateCommitsDetectedStatement { get; }
        String EnsureTimestampIndexCreatedStatement { get; }
        String EnsureTimestampIndexDroppedStatement { get; }

        DbParameter CreateTimestampParameter(DateTime timestamp);
        DbParameter CreateCommitIdParameter(Guid commitId);
        DbParameter CreateStreamIdParameter(Guid streamId);
        DbParameter CreateVersionParameter(Int32 version);
        DbParameter CreateHeadersParameter(Byte[] headers);
        DbParameter CreateEventsParameter(Byte[] events);
        DbParameter CreateSkipParameter(Int32 skip);
        DbParameter CreateTakeParameter(Int32 take);
    }
}
