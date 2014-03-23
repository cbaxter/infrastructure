using System;

namespace Spark.Cqrs.Domain
{
    /// <summary>
    /// A base for strongly typed <see cref="Entity"/> unique identifiers.
    /// </summary>
    public abstract class EntityId : ValueObject<Guid>
    {
        /// <summary>
        /// Initialize a new instance of <see cref="EntityId"/>.
        /// </summary>
        protected EntityId()
            : base(GuidStrategy.NewGuid())
        { }

        /// <summary>
        /// Initialize an existing instance of <see cref="EntityId"/>.
        /// </summary>
        /// <param name="value">The underlying entity identifier.</param>
        protected EntityId(Guid value)
            : base(value)
        { }

        /// <summary>
        /// Attempt to parse the <see cref="String"/> <paramref name="value"/> in to the base value type to be wrapped by a value object instance.
        /// </summary>
        /// <param name="value">The value to parse.</param>
        /// <param name="result">The parsed instance of the underlying value type.</param>
        protected override Boolean TryParse(String value, out Guid result)
        {
            return Guid.TryParse(value, out result);
        }
    }
}
