﻿using System;
using System.Data;

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

namespace Spark.EventStore.Sql
{
    /// <summary>
    /// Extension methods of <see cref="IDataRecord"/>.
    /// </summary>
    public static class DataRecordExtensions
    {
        /// <summary>
        /// Return the value of the specified field.
        /// </summary>
        /// <param name="dataRecord">The data record containing the field to find.</param>
        /// <param name="i">The index of the field to find.</param>
        public static Byte[] GetBytes(this IDataRecord dataRecord, Int32 i)
        {
            Verify.NotNull(dataRecord, "dataRecord");

            return (Byte[])dataRecord.GetValue(i);
        }
    }
}
