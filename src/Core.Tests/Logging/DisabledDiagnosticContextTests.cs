﻿using System;
using System.Diagnostics;
using Spark.Infrastructure.Logging;
using Xunit;

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

namespace Spark.Infrastructure.Tests.Logging
{
    public static class UsingDisabledDiagnosticContext
    {
        public class WhenDisposing
        {
            [Fact]
            public void CanDisposeRepeatedly()
            {
                using (DisabledDiagnosticContext.Instance)
                    DisabledDiagnosticContext.Instance.Dispose();
            }

            [Fact]
            public void PerfTest()
            {
                var values = new Int32[0];
                var alwaysFalse = values.Length == 0;

                var sw = Stopwatch.StartNew();

                for (var i = 0; i < 1000000000; i++)
                {
                  
                        foreach (var value in values)
                            Console.WriteLine(value);
                 
                }

                sw.Stop();

                Console.WriteLine(sw.ElapsedMilliseconds);

            }
        }
    }
}
