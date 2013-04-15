using System;

namespace Spark.Infrastructure
{
    public static class SequentialGuid
    {
        public static Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}
