﻿using System;
using Spark.Configuration;

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

namespace Spark.Tests.Configuration
{
    public sealed class CommandProcessorSettings : IProcessCommandSettings
    {
        public Int32 BoundedCapacity { get; set; }
        public Int32 MaximumConcurrencyLevel { get; set; }
        public TimeSpan RetryTimeout { get; set; }

        public CommandProcessorSettings()
        {
            BoundedCapacity = 100;
            MaximumConcurrencyLevel = 10;
            RetryTimeout = TimeSpan.FromMilliseconds(100);
        }
    }
}
