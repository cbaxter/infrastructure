using System;
using System.IO;

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
    /// Serializes or Deserializes an object graph to or from the provided <see cref="Stream"/>.
    /// </summary>
    public interface ISerializeObjects
    {
        /// <summary>
        /// Serializes the object graph to the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> on which to serialize the object <paramref name="graph"/>.</param>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="type">The <see cref="Type"/> of object being serialized.</param>
        void Serialize(Stream stream, Object graph, Type type);

        /// <summary>
        /// Deserialize an object graph from the speciied <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> from which to deserialize an object graph.</param>
        /// <param name="type">The <see cref="Type"/> of object being deserialized.</param>
        Object Deserialize(Stream stream, Type type);
    }

    /// <summary>
    /// Extension methods of <see cref="ISerializeObjects"/>
    /// </summary>
    public static class SerializeObjectExtensions
    {
        /// <summary>
        /// Serializes the object graph to the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> implementation being extended.</param>
        /// <param name="stream">The <see cref="Stream"/> on which to serialize the object <paramref name="graph"/>.</param>
        /// <param name="graph">The object to serialize.</param>
        public static void Serialize<T>(this ISerializeObjects serializer, Stream stream, T graph)
        {
            Verify.NotNull(serializer, "serializer");

            serializer.Serialize(stream, graph, typeof(T));
        }

        /// <summary>
        /// Serializes an object graph to binary data.
        /// </summary>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> implementation being extended.</param>
        /// <param name="graph">The object graph to serialize.</param>
        public static Byte[] Serialize<T>(this ISerializeObjects serializer, T graph)
        {
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, graph);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Deserialize an object graph from the speciied <paramref name="stream"/>.
        /// </summary>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> implementation being extended.</param>
        /// <param name="stream">The <see cref="Stream"/> from which to deserialize an object graph.</param>
        public static T Deserialize<T>(this ISerializeObjects serializer, Stream stream)
        {
            Verify.NotNull(serializer, "serializer");

            return (T)serializer.Deserialize(stream, typeof(T));
        }

        /// <summary>
        /// Deserializes a binary field in to an object graph.
        /// </summary>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> implementation being extended.</param>
        /// <param name="buffer">The binary data to be deserialized in to an object graph.</param>
        public static T Deserialize<T>(this ISerializeObjects serializer, Byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer, writable: false))
            {
                var result = serializer.Deserialize<T>(stream);

                return result;
            }
        }

        /// <summary>
        /// Deserializes a binary field in to an object graph.
        /// </summary>
        /// <param name="serializer">The <see cref="ISerializeObjects"/> implementation being extended.</param>
        /// <param name="buffer">The binary data to be deserialized in to an object graph.</param>
        /// <param name="type">The <see cref="Type"/> of object being deserialized.</param>
        public static Object Deserialize(this ISerializeObjects serializer, Byte[] buffer, Type type)
        {
            using (var stream = new MemoryStream(buffer, writable: false))
            {
                var result = serializer.Deserialize(stream, type);

                return result;
            }
        }
    }
}
