using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Spark.Infrastructure.Domain;
using Xunit;

namespace Spark.Infrastructure.Tests
{
    public static class ObjectSerializerTests
    {
        public class WhenSerializingObject
        {
            [Fact]
            public void IncludeMutableFields()
            {
                var graph = new DerivedClass();
                var dictionary = ObjectSerializer.Serialize(graph);
            }
        }


        public class BaseClass
        {
            [IgnoreDataMember]
            public Decimal BaseIgnoreDataMemberAutoProperty { get; set; }

            public Decimal BaseAutoProperty { get; set; }

            public readonly Int32 BaseImmutableField = 3;

            [DataMember(Name = "a", Order = 2)]
            public Int32 BaseDatamemberNamedFieldA = 4;

            [DataMember(Name = "n", Order = 1)]
            public Int32 BaseDataMemberNamedFieldN = 5;

            [IgnoreDataMember]
            public Int32 BaseIgnoredField = 6;

            public Int32 BaseMutableField = 7;

            [DataMember(EmitDefaultValue = true)]
            public Int32 BaseDefaultValueField;

            public Int32 BaseIgnoredDefaultValueField;

            public BaseClass()
            {
                BaseIgnoreDataMemberAutoProperty = 1;
                BaseAutoProperty = 2;
            }
        }

        public class DerivedClass : BaseClass
        {
            [IgnoreDataMember]
            public Decimal DerivedIgnoreDataMemberAutoProperty { get; set; }

            public Decimal DerivedAutoProperty { get; set; }

            public readonly Int32 DerivedImmutableField = 3;

            [DataMember(Name = "a2", Order = 2)]
            public Int32 DerivedDatamemberNamedFieldA = 4;

            [DataMember(Name = "n2", Order = 1)]
            public Int32 DerivedDataMemberNamedFieldN = 5;

            [IgnoreDataMember]
            public Int32 DerivedIgnoredField = 6;

            public Int32 DerivedMutableField = 7;

            [DataMember(EmitDefaultValue = true)]
            public Int32 DerivedDefaultValueField;

            public Int32 DerivedIgnoredDefaultValueField;

            public DerivedClass()
            {
                DerivedIgnoreDataMemberAutoProperty = 1;
                DerivedAutoProperty = 2;
            }
        }
    }
}
