using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

namespace Spark.Serialization
{
    /// <summary>
    /// An <see cref="IContractResolver"/> implementation that assigns a default <see cref="JsonConverter"/> for a given <see cref="JsonContract"/>.
    /// </summary>
    /// <remarks>
    /// Optimization to ensure that the set of <see cref="JsonConverter"/> instances are not enumerated on each serialize/deserialize call.
    /// </remarks>
    public sealed class ConverterContractResolver : DefaultContractResolver
    {
        private readonly IList<JsonConverter> knownConverters;
        
        /// <summary>
        /// Initializes a new instance of <see cref="ConverterContractResolver"/> using the set of specified <see cref="JsonConverter"/> objects.
        /// </summary>
        /// <param name="jsonConverters"></param>
        public ConverterContractResolver(IEnumerable<JsonConverter> jsonConverters)
        {
            knownConverters = typeof(ConverterContractResolver).Assembly
                                                               .GetTypes()
                                                               .Where(type => !type.IsAbstract && type.IsClass && type.DerivesFrom(typeof(JsonConverter)))
                                                               .Select(type => (JsonConverter)Activator.CreateInstance(type)).Concat(jsonConverters.EmptyIfNull())
                                                               .OrderByDescending(converter => converter.CanRead && converter.CanWrite)
                                                               .ThenBy(converter => converter.GetType().FullName)
                                                               .ToList();
        }

        /// <summary>
        /// Determine which contract type is created for the given <paramref name="objectType"/>.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);

            // Assigns the first JsonConverter that can convert the specified object type favoring converts that support Read and Write over just Read or Write.
            contract.Converter = knownConverters.FirstOrDefault(converter => converter.CanConvert(objectType));

            return contract;
        }
    }
}
