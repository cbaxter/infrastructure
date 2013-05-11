using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;

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
    /// Indicates that a field of a class should not be included in a <see cref="ObjectHasher"/> hash.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class NonHashedAttribute : Attribute { }

    /// <summary>
    /// Computes a MD5 hash on any non-recursive object graph.
    /// </summary>
    public static class ObjectHasher
    {
        private static readonly IDictionary<Type, Action<Object, Stream>> Hashers = new ConcurrentDictionary<Type, Action<Object, Stream>>();
        private static readonly MethodInfo HashObjectMethod = typeof(ObjectHasher).GetMethod("HashObject", BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Computes a MD5 hash on a non-recursive object graph.
        /// </summary>
        /// <param name="value">The non-recursive object graph for which a hash is to be computed.</param>
        public static Guid Hash(Object value)
        {
            using (var stream = new MemoryStream())
            {
                HashObject(value, stream);

                stream.Position = 0;
                using (var provider = new MD5CryptoServiceProvider())
                    return new Guid(provider.ComputeHash(stream));
            }
        }

        /// <summary>
        /// Computes a MD5 hash on <paramref name="value"/> based on type.
        /// </summary>
        /// <param name="value">The non-recursive object graph for which a hash is to be computed.</param>
        /// <param name="stream">The <see cref="Stream"/> to write value hash codes.</param>
        private static void HashObject(Object value, Stream stream)
        {
            if (value == null)
            {
                stream.WriteByte(0);
            }
            else
            {
                var type = value.GetType();
                if (type.IsValueType || typeof(String).IsAssignableFrom(type))
                {
                    stream.Write(BitConverter.GetBytes(value.GetHashCode()));
                }
                else
                {
                    if (type.IsArray)
                    {
                        HashArray((Array)value, stream);
                    }
                    else
                    {
                        HashReference(value, stream);
                    }
                }
            }
        }

        /// <summary>
        /// Compute a MD5 hash on a set of array values.
        /// </summary>
        /// <param name="value">The array for which a hash is to be computed.</param>
        /// <param name="stream">The <see cref="Stream"/> to write value hash codes.</param>
        private static void HashArray(Array value, Stream stream)
        {
            if (value.Rank == 1)
            {
                for (var i = 0; i < value.Length; i++)
                    HashObject(value.GetValue(i), stream);
            }
            else
            {
                var lengths = new Int32[value.Rank];
                for (var dimension = 0; dimension < value.Rank; dimension++)
                    lengths[dimension] = value.GetLength(dimension);

                HashArrayValues(value, stream, lengths, new Int32[value.Rank], 0);
            }
        }

        /// <summary>
        /// Compute a MD5 hash on a set of multi-dimensonal array values.
        /// </summary>
        /// <param name="value">The value array.</param>
        /// <param name="stream">The <see cref="Stream"/> to write value hash codes.</param>
        /// <param name="lengths">The set of array lengths that make up the multi-dimensional array.</param>
        /// <param name="indicies">The indicies of the multi-dimensional array to be processed.</param>
        /// <param name="dimension">The current array dimension being processed.</param>
        private static void HashArrayValues(Array value, Stream stream, Int32[] lengths, Int32[] indicies, Int32 dimension)
        {
            if (dimension == value.Rank - 1)
            {
                indicies[dimension] = 0;
                while (indicies[dimension] < lengths[dimension])
                {
                    HashObject(value.GetValue(indicies), stream);
                    indicies[dimension]++;
                }
            }
            else
            {
                indicies[dimension] = 0;
                while (indicies[dimension] < lengths[dimension])
                {
                    HashArrayValues(value, stream, lengths, indicies, dimension + 1);
                    indicies[dimension]++;
                }
            }
        }

        /// <summary>
        /// Compute a MD5 hash on a set of object fields.
        /// </summary>
        /// <param name="value">The non-recursive object graph for which a hash is to be computed.</param>
        /// <param name="stream">The <see cref="Stream"/> to write value hash codes.</param>
        private static void HashReference(Object value, Stream stream)
        {
            var type = value.GetType();

            Action<Object, Stream> hasher;
            if (!Hashers.TryGetValue(type, out hasher))
                Hashers[type] = hasher = CompileObjectHasher(type);

            hasher.Invoke(value, stream);
        }

        /// <summary>
        /// Create an object hash method
        /// </summary>
        /// <param name="type">The object type for which a hash method is to be created.</param>
        private static Action<Object, Stream> CompileObjectHasher(Type type)
        {
            var stream = Expression.Parameter(typeof(Stream), "stream");
            var rawValue = Expression.Parameter(typeof(Object), "value");
            var typedValue = Expression.Parameter(type, "typedValue");

            return Expression.Lambda<Action<Object, Stream>>(Expression.Block(new[] { typedValue }, GetMethodBody(stream, rawValue, typedValue)), rawValue, stream).Compile();
        }

        /// <summary>
        /// Determins the set of expressions that comprise an object factory method's body.
        /// </summary>
        /// <param name="stream">The stream argument to which hash codes are to be written.</param>
        /// <param name="rawValue">The object type for which a hash method is to be created.</param>
        /// <param name="typedValue">The typed <paramref name="rawValue"/> variable that represents the object to hash.</param>
        private static IEnumerable<Expression> GetMethodBody(ParameterExpression stream, ParameterExpression rawValue, ParameterExpression typedValue)
        {
            var type = typedValue.Type;

            yield return Expression.Assign(typedValue, Expression.Convert(rawValue, type));

            while (type != typeof(Object) && type != null)
            {
                var fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<NonHashedAttribute>() == null)
                        yield return Expression.Call(HashObjectMethod, Expression.Convert(Expression.Field(typedValue, field), typeof(Object)), stream);
                }

                type = type.BaseType;
            }
        }
    }
}
