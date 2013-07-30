﻿using System;

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

namespace Spark.Logging
{
    /// <summary>
    /// Disabled diagnostics context for use when suppressing activity tracking.
    /// </summary>
    public sealed class DisabledDiagnosticContext : IDisposable
    {
        /// <summary>
        /// <see cref="DisabledDiagnosticContext" /> singleton instance.
        /// </summary>
        public static readonly IDisposable Instance = new DisabledDiagnosticContext();

        /// <summary>
        /// Initialize a new instance of <see cref="DisabledDiagnosticContext"/>.
        /// </summary>
        private DisabledDiagnosticContext()
        { }

        /// <summary>
        /// Releases all managed resources used by the current instance of the <see cref="DisabledDiagnosticContext"/> class.
        /// </summary>
        public void Dispose()
        { }
    }
}
