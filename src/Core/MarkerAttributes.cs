using System;

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

namespace Spark
{
    /// <summary>
    /// Marks a resolved method property as being a transient instance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public sealed class TransientAttribute : Attribute
    { }

    /// <summary>
    /// Marks a resolved method property as being a singleton instance (default).
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public sealed class SingletonAttribute : Attribute
    { }
}
