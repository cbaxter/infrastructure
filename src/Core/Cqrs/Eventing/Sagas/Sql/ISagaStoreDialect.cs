﻿using System;
using System.Data;
using Spark.Data;

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

namespace Spark.Cqrs.Eventing.Sagas.Sql
{
    public interface ISagaStoreDialect : IDbDialect
    {
        String EnsureSagaTableExists { get; }

        String GetSaga { get; }
        String GetScheduledTimeouts { get; }
        String InsertSaga { get; }
        String UpdateSaga { get; }
        String DeleteSaga { get; }
        String DeleteSagas { get; }

        IDataParameter CreateIdParameter(Guid sagaId);
        IDataParameter CreateTypeIdParameter(Guid sagaType);
        IDataParameter CreateVersionParameter(Int32 version);
        IDataParameter CreateTimeoutParameter(DateTime? timeout);
        IDataParameter CreateStateParameter(Byte[] state);
    }
}
