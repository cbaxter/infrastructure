using System;
using System.Collections;
using System.Collections.Generic;

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
using System.Linq;

namespace Spark.Cqrs.Domain
{
    /// <summary>
    /// A collection of <see cref="Entity"/> objects uniquely identified by <see cref="Entity.Id"/>.
    /// </summary>
    public abstract class EntityCollection : IEnumerable<Entity>
    {
        /// <summary>
        /// Get the backing <see cref="IDictionary"/> instance.
        /// </summary>
        protected abstract IDictionary Dictionary { get; }

        /// <summary>
        /// Get the set of entities currently stored within this <see cref="EntityCollection"/> instance.
        /// </summary>
        protected abstract IEnumerable<Entity> Entities { get; }

        /// <summary>
        /// Get the base <see cref="Entity"/> <see cref="Type"/>.
        /// </summary>
        public abstract Type EntityType { get; }

        /// <summary>
        /// Gets the number of entities contained in the <see cref="EntityCollection"/>.
        /// </summary>
        public abstract Int32 Count { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="EntityCollection"/>.
        /// </summary>
        internal EntityCollection()
        { }

        /// <summary>
        /// Add the specified <see cref="Entity"/> to the collection.
        /// </summary>
        /// <param name="entity">The entity to add to the collection.</param>
        public void Add(Entity entity)
        {
            Verify.NotNull(entity, "entity");

            Dictionary.Add(entity.Id, entity);
        }

        /// <summary>
        /// Determines whether the <see cref="EntityCollection"/> contains the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique entity identifier.</param>
        public abstract Boolean Contains(Guid id);

        /// <summary>
        /// Determines whether the <see cref="EntityCollection"/> contains the specified <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public Boolean Contains(Entity entity)
        {
            return entity != null && Contains(entity.Id);
        }

        /// <summary>
        /// Removes the <see cref="Entity"/> with the specified <paramref name="id"/> from the <see cref="EntityCollection"/>.
        /// </summary>
        /// <param name="id">The unique entity identifier to remove.</param>
        public abstract Boolean Remove(Guid id);

        /// <summary>
        /// Removes the <see cref="Entity"/> from the <see cref="EntityCollection"/>.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        public Boolean Remove(Entity entity)
        {
            return entity != null && Remove(entity.Id);
        }

        /// <summary>
        /// Removes all entities from the collection.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Returns an enumerator that iterates through the entity collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Entities.GetEnumerator();
        }

        /// <summary>
        /// Returns a strongly typed enumerator that iterates through the entity collection.
        /// </summary>
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator()
        {
            return Entities.GetEnumerator();
        }
    }

    /// <summary>
    /// A collection of <see cref="Entity"/> objects uniquely identified by <see cref="Entity.Id"/>.
    /// </summary>
    public sealed class EntityCollection<TEntity> : EntityCollection, ICollection<TEntity>, IReadOnlyCollection<TEntity>, IReadOnlyDictionary<Guid, TEntity>
        where TEntity : Entity
    {
        private readonly Dictionary<Guid, TEntity> dictionary = new Dictionary<Guid, TEntity>();

        /// <summary>
        /// Get the backing <see cref="IDictionary"/> instance.
        /// </summary>
        protected override IDictionary Dictionary { get { return dictionary; } }

        /// <summary>
        /// Get the set of entities currently stored within this <see cref="EntityCollection"/> instance.
        /// </summary>
        protected override IEnumerable<Entity> Entities { get { return dictionary.Values; } }

        /// <summary>
        /// Get the base <see cref="Entity"/> <see cref="Type"/>.
        /// </summary>
        public override Type EntityType { get { return typeof(TEntity); } }

        /// <summary>
        /// Gets the number of entities contained in the <see cref="EntityCollection"/>.
        /// </summary>
        public override Int32 Count { get { return dictionary.Count; } }
        
        /// <summary>
        /// Gets the <see cref="Entity"/> identified by the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique entity identifier.</param>
        public TEntity this[Guid id] { get { return dictionary[id]; } }

        /// <summary>
        /// Initializes a new instance of <see cref="EntityCollection"/>.
        /// </summary>
        public EntityCollection()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="EntityCollection"/> populated with the specified <paramref name="entities"/>.
        /// </summary>
        public EntityCollection(IEnumerable<TEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException("entities");

            foreach (var entity in entities.Where(entity => entity != null))
                dictionary.Add(entity.Id, entity);
        }

        /// <summary>
        /// Add the specified <see cref="Entity"/> to the collection.
        /// </summary>
        /// <param name="entity">The entity to add to the collection.</param>
        public void Add(TEntity entity)
        {
            Verify.NotNull(entity, "entity");

            dictionary.Add(entity.Id, entity);
        }

        /// <summary>
        /// Determines whether the <see cref="EntityCollection"/> contains the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique entity identifier.</param>
        public override Boolean Contains(Guid id)
        {
            return dictionary.ContainsKey(id);
        }

        /// <summary>
        /// Determines whether the <see cref="EntityCollection"/> contains the specified <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public Boolean Contains(TEntity entity)
        {
            return dictionary.ContainsKey(entity.Id);
        }

        /// <summary>
        /// Gets an existing <see cref="Entity"/> if exists within the <see cref="EntityCollection"/>; otherwise adds and returns a using
        /// the specified <paramref name="factory"/> method.
        /// </summary>
        /// <param name="id">The unique entity identifier.</param>
        /// <param name="factory">The <see cref="Entity"/> factory methods.</param>
        public TEntity GetOrAdd
            (Guid id, Func<Guid, TEntity> factory)
        {
            TEntity entity;
            if (TryGet(id, out entity))
                return entity;

            Add(entity = factory.Invoke(id));

            return entity;
        }

        /// <summary>
        /// Gets the <see cref="Entity"/> associated with the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id of the entity to get.</param>
        /// <param name="entity"></param>
        public Boolean TryGet(Guid id, out TEntity entity)
        {
            return dictionary.TryGetValue(id, out entity);
        }

        /// <summary>
        /// Removes the <see cref="Entity"/> with the specified <paramref name="id"/> from the <see cref="EntityCollection"/>.
        /// </summary>
        /// <param name="id">The unique entity identifier to remove.</param>
        public override Boolean Remove(Guid id)
        {
            return dictionary.Remove(id);
        }

        /// <summary>
        /// Removes the <see cref="Entity"/> from the <see cref="EntityCollection"/>.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        public Boolean Remove(TEntity entity)
        {
            return entity != null && dictionary.Remove(entity.Id);
        }

        /// <summary>
        /// Removes all entities from the collection.
        /// </summary>
        public override void Clear()
        {
            dictionary.Clear();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="EntityCollection"/>.
        /// </summary>
        public IEnumerator<TEntity> GetEnumerator()
        {
            return dictionary.Values.GetEnumerator();
        }

        /// <summary>
        /// Copies the <see cref="EntityCollection"/> to an existing one-dimensional <see cref="Array"/>, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="EntityCollection"/>.  The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(TEntity[] array, Int32 index)
        {
            dictionary.Values.CopyTo(array, index);
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="EntityCollection"/> is read-only.
        /// </summary>
        Boolean ICollection<TEntity>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Determines whether the read-only dictionary contains an element that has the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        Boolean IReadOnlyDictionary<Guid, TEntity>.ContainsKey(Guid key)
        {
            return Contains(key);
        }

        /// <summary>
        /// Gets an enumerable collection that contains the keys in the read-only dictionary.
        /// </summary>
        IEnumerable<Guid> IReadOnlyDictionary<Guid, TEntity>.Keys
        {
            get { return dictionary.Keys; }
        }

        /// <summary>
        /// Gets the value that is associated with the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        Boolean IReadOnlyDictionary<Guid, TEntity>.TryGetValue(Guid key, out TEntity value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets an enumerable collection that contains the values in the read-only dictionary.
        /// </summary>
        IEnumerable<TEntity> IReadOnlyDictionary<Guid, TEntity>.Values
        {
            get { return dictionary.Values; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="EntityCollection"/> key/value pairs.
        /// </summary>
        IEnumerator<KeyValuePair<Guid, TEntity>> IEnumerable<KeyValuePair<Guid, TEntity>>.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }
    }
}
