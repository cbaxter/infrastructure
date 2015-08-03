using System;
using System.Runtime.Serialization;
using Spark.Cqrs.Domain;
using Spark.EventStore;
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

namespace Test.Spark.Cqrs.Domain
{
    namespace UsingAggregateStateValidator
    {
        public class WhenInvokingPostGet
        {
            [Fact]
            public void StepThroughIfAggregateHashNotSetd()
            {
                var pipelineHook = new AggregateStateValidator();
                var aggregate = new FakeAggregate();

                pipelineHook.PostGet(aggregate);
            }

            [Fact]
            public void StepThroughIfAggregateHashUnchanged()
            {
                var pipelineHook = new AggregateStateValidator();
                var aggregate = new FakeAggregate();

                aggregate.UpdateHash();

                pipelineHook.PostGet(aggregate);
            }

            [Fact]
            public void ThrowMemberAccessExceptionIfHashInvalid()
            {
                var pipelineHook = new AggregateStateValidator();
                var aggregate = new FakeAggregate();

                aggregate.UpdateHash();
                aggregate.State = Guid.NewGuid();

                Assert.Throws<MemberAccessException>(() => pipelineHook.PostGet(aggregate));
            }
        }

        public class WhenInvokingPreSave
        {
            [Fact]
            public void StepThroughIfAggregateHashNotSetd()
            {
                var pipelineHook = new AggregateStateValidator();
                var aggregate = new FakeAggregate();

                pipelineHook.PreSave(aggregate, null);
            }

            [Fact]
            public void StepThroughIfAggregateHashUnchanged()
            {
                var pipelineHook = new AggregateStateValidator();
                var aggregate = new FakeAggregate();

                aggregate.UpdateHash();

                pipelineHook.PreSave(aggregate, null);
            }

            [Fact]
            public void ThrowMemberAccessExceptionIfHashInvalid()
            {
                var pipelineHook = new AggregateStateValidator();
                var aggregate = new FakeAggregate();

                aggregate.UpdateHash();
                aggregate.State = Guid.NewGuid();

                Assert.Throws<MemberAccessException>(() => pipelineHook.PreSave(aggregate, null));
            }
        }

        public class WhenInvokingPostSave
        {
            [Fact]
            public void StepThroughOnErrorIfAggregateHashUnchanged()
            {
                var pipelineHook = new AggregateStateValidator();
                var aggregate = new FakeAggregate();

                aggregate.UpdateHash();

                pipelineHook.PostSave(aggregate, null, new InvalidOperationException());

                aggregate.VerifyHash();
            }

            [Fact]
            public void ThrowMemberAccessExceptionOnErrorIfHashInvalid()
            {
                var pipelineHook = new AggregateStateValidator();
                var aggregate = new FakeAggregate();

                aggregate.UpdateHash();
                aggregate.State = Guid.NewGuid();
                
                Assert.Throws<MemberAccessException>(() => pipelineHook.PostSave(aggregate, null, new InvalidOperationException()));
            }

            [Fact]
            public void UpdateHashOnSuccessfulSave()
            {
                var commit = (Commit)FormatterServices.GetUninitializedObject(typeof(Commit));
                var pipelineHook = new AggregateStateValidator();
                var aggregate = new FakeAggregate();

                aggregate.UpdateHash();
                aggregate.State = Guid.NewGuid();

                pipelineHook.PostSave(aggregate, commit, null);

                aggregate.VerifyHash();
            }
        }

        internal class FakeAggregate : Aggregate
        {
            public Guid State { get; set; }
        }
    }
}
