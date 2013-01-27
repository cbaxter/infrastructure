using System;

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

namespace Spark.Infrastructure
{
    /// <summary>
    /// Gets the current UTC system time.
    /// </summary>
    /// <remarks>Can override usage of <see cref="DateTime.UtcNow"/> by calling <see cref="OverrideWith"/>.</remarks>
    public static class SystemTime
    {
        private static Func<DateTime> now;

        /// <summary>
        /// Get the current system time (UTC).
        /// </summary>
        public static DateTime Now { get { return now == null ? DateTime.UtcNow : now(); } }

        /// <summary>
        /// Override the use of <see cref="DateTime.UtcNow"/> with a custom time function.
        /// </summary>
        /// <param name="timeRetriever">The replacement function to use when retrieving the UTC system time.</param>
        public static void OverrideWith(Func<DateTime> timeRetriever)
        {
            Verify.NotNull(timeRetriever, "timeRetriever");
            Verify.Equal(DateTimeKind.Utc, timeRetriever().Kind, "timeRetriever");

            now = timeRetriever;
        }

        /// <summary>
        /// Clear the overriden system time function.
        /// </summary>
        public static void ClearOverride()
        {
            now = null;
        }
    }
}
