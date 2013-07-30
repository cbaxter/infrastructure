using System;

namespace Spark
{
    /// <summary>
    /// he guid strategy to be used when creating <see cref="Guid"/> structures.
    /// </summary>
    public static class GuidStrategy
    {
        private static Func<Guid> guidFactory = Guid.NewGuid;

        /// <summary>
        /// The guid strategy to be used when creating <see cref="Guid"/> structures.
        /// </summary>
        /// <param name="guidGenerator"></param>
        public static void Initialize(Func<Guid> guidGenerator)
        {
            Verify.NotNull(guidGenerator, "guidGenerator");

            guidFactory = guidGenerator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Guid"/> structure.
        /// </summary>
        public static Guid NewGuid()
        {
            return guidFactory();
        }
    }
}
