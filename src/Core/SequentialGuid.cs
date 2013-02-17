using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
