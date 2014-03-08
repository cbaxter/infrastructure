using System;
using System.Data.SqlClient;
using System.Reflection;
using Spark;
using Spark.Data;
using Spark.Data.SqlClient;
using Xunit;

namespace Test.Spark.Data.SqlClient
{
    public static class UsingSqlTransientErrorRegistry
    {
        public class WhenCheckingIfExceptionIsTransient
        {
            private readonly IDetectTransientErrors transientErrorRegistry = new SqlTransientErrorRegistry();

            [Fact]
            public void ConcurrencyExceptionIsAlwaysTransient()
            {
                Assert.True(transientErrorRegistry.IsTransient(new ConcurrencyException()));
            }
            [Fact]
            public void SqlExceptionIsTransientIfDeadlock()
            {
                var ex = CreateException(SqlErrorCode.Deadlock);

                Assert.True(transientErrorRegistry.IsTransient(ex));
            }
        }

        private static SqlException CreateException(Int32 errorCode)
        {
            var collection = Construct<SqlErrorCollection>(Type.EmptyTypes);
            var error = Construct<SqlError>(new[] { typeof(Int32), typeof(Byte), typeof(Byte), typeof(String), typeof(String), typeof(String), typeof(Int32) }, errorCode, (Byte)2, (Byte)3, "ServerName", "Message", "Process", 100);

            typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(collection, new object[] { error });

            return (SqlException)typeof(SqlException).GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(SqlErrorCollection), typeof(String) }, null).Invoke(null, new object[] { collection, "7.0.0" }) as SqlException;
        }

        private static T Construct<T>(Type[] types, params object[] p)
        {
            return (T)typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, types, null).Invoke(p);
        }
    }
}
