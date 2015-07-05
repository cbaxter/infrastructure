using System;
using Spark.Cqrs.Commanding;
using Spark.Cqrs.Domain;
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
    namespace UsingAggregate
    {
        public class WhenVerifyingUninitialized
        {
            [Fact]
            public void CanNotHandleCommandIfVersionGreaterThanZero()
            {
                var aggregate = new FakeAggregate(version: 1);
                var command = new FakeCommand();

                Assert.Throws<InvalidOperationException>(() => aggregate.Handle(command));
            }

            [Fact]
            public void CanHandleCommandIfVersionEqualsZero()
            {
                var aggregate = new FakeAggregate(version: 0);
                var command = new FakeCommand();

                aggregate.Handle(command);
            }

            internal class FakeAggregate : Aggregate
            {
                protected override bool RequiresExplicitCreate { get { return false; } }

                public FakeAggregate(Int32 version)
                {
                    Version = version;
                }

                public void Handle(FakeCommand command)
                {
                    VerifyUninitialized();
                }
            }
        }

        public class WhenVerifyingInitialized
        {
            [Fact]
            public void CanNotHandleCommandIfVersionEqualsZero()
            {
                var aggregate = new FakeAggregate(version: 0);
                var command = new FakeCommand();

                Assert.Throws<InvalidOperationException>(() => aggregate.Handle(command));
            }

            [Fact]
            public void CanHandleCommandIfVersionGreaterThanZero()
            {
                var aggregate = new FakeAggregate(version: 1);
                var command = new FakeCommand();

                aggregate.Handle(command);
            }

            internal class FakeAggregate : Aggregate
            {
                protected override bool RequiresExplicitCreate { get { return false; } }

                public FakeAggregate(Int32 version)
                {
                    Version = version;
                }

                public void Handle(FakeCommand command)
                {
                    VerifyInitialized();
                }
            }
        }

        internal class FakeCommand : Command
        { }
    }
}
