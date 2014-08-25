using System;
using System.Data.SqlClient;
using System.Reflection;
using Spark;
using Spark.Data;
using Spark.Data.SqlClient;
using Xunit;

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

namespace Test.Spark.Data.SqlClient
{
    namespace UsingSqlTransientErrorRegistry
    {
        public class WhenCheckingIfExceptionIsTransient
        {
            private readonly IDetectTransientErrors transientErrorRegistry = new SqlTransientErrorRegistry();

            [Fact]
            public void ConcurrencyExceptionIsAlwaysTransient()
            {
                Assert.True(transientErrorRegistry.IsTransient(new ConcurrencyException()));
            }
            [Fact]
            public void SqlExceptionIsTransientIfDeadlock()
            {
                var ex = CreateException(SqlErrorCode.Deadlock);

                Assert.True(transientErrorRegistry.IsTransient(ex));
            }

            internal static SqlException CreateException(Int32 errorCode)
            {
                var collection = Construct<SqlErrorCollection>(Type.EmptyTypes);
                var error = Construct<SqlError>(new[] { typeof(Int32), typeof(Byte), typeof(Byte), typeof(String), typeof(String), typeof(String), typeof(Int32) }, errorCode, (Byte)2, (Byte)3, "ServerName", "Message", "Process", 100);

                typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(collection, new object[] { error });

                return (SqlException)typeof(SqlException).GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(SqlErrorCollection), typeof(String) }, null).Invoke(null, new object[] { collection, "7.0.0" }) as SqlException;
            }

            internal static T Construct<T>(Type[] types, params object[] p)
            {
                return (T)typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, types, null).Invoke(p);
            }
        }
    }
}
