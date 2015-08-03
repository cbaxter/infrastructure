using Spark.Cqrs.Eventing;
using Spark.EventStore;
using Spark.Messaging;
using Xunit;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Test.Spark.Serialization.Converters
{
    namespace UsingCommitDataConverter
    {
        public class WhenWritingJson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeNullValue()
            {
                var json = WriteJson(default(CommitData));

                Validate(json, @"
{
  ""h"": {},
  ""e"": []
}");
            }

            [Fact]
            public void CanSerializeToJson()
            {
                var data = new CommitData(HeaderCollection.Empty, EventCollection.Empty);
                var json = WriteJson(data);

                Validate(json, @"
{
  ""h"": {},
  ""e"": []
}");
            }
        }

        public class WhenReadingJson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeNull()
            {
                Assert.Equal(CommitData.Empty, ReadJson<CommitData>("null"));
            }

            [Fact]
            public void CanDeserializeValidJson()
            {
                var data = ReadJson<CommitData>(@"
{
  ""h"": {},
  ""e"": []
}");
                Assert.Equal(HeaderCollection.Empty, data.Headers);
            }

            [Fact]
            public void PropertyOrderIrrelevant()
            {
                var data = ReadJson<CommitData>(@"
{
  ""e"": [],
  ""h"": {}
}");
                Assert.Equal(HeaderCollection.Empty, data.Headers);
            }

            [Fact]
            public void CanTolerateMalformedJson()
            {
                var data = ReadJson<CommitData>(@"
{
  ""h"": {},
  ""e"": [],
}");
                Assert.Equal(HeaderCollection.Empty, data.Headers);
            }
        }

        public class WhenWritingBson : UsingJsonConverter
        {
            [Fact]
            public void CanSerializeToBson()
            {
                var data = new CommitData(HeaderCollection.Empty, EventCollection.Empty);
                var json = WriteBson(data);

                Validate("﻿FQAAAANoAAUAAAAABGUABQAAAAAA", json);
            }
        }

        public class WhenReadingBson : UsingJsonConverter
        {
            [Fact]
            public void CanDeserializeValidBson()
            {
                var bson = "FQAAAANoAAUAAAAABGUABQAAAAAA";
                var data = ReadBson<CommitData>(bson);

                Assert.Equal(HeaderCollection.Empty, data.Headers);
            }
        }
    }
}
