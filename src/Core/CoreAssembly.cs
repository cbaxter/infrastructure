using System;
using System.Reflection;

namespace Spark
{
    /// <summary>
    /// Core assembly reference/version.
    /// </summary>
    internal static class CoreAssembly
    {
        /// <summary>
        /// The core assembly reference.
        /// </summary>
        public static readonly Assembly Reference = typeof(CoreAssembly).Assembly;

        /// <summary>
        /// The core assembly version.
        /// </summary>
        public static readonly Version Version = Reference.GetName().Version;
    }
}
