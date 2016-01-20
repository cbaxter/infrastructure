using System;
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

namespace Test.Spark.Messaging
{
    namespace UsingMessage
    {
        public class WhenCreatingMessage
        {
            [Fact]
            public void IdentifierCannotBeEmptyGuid()
            {
                var ex = Assert.Throws<ArgumentException>(() => new Message<Object>(Guid.Empty, HeaderCollection.Empty, new Object()));

                Assert.Equal("id", ex.ParamName);
            }

            [Fact]
            public void HeadersCollectionCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new Message<Object>(Guid.NewGuid(), null, new Object()));

                Assert.Equal("headers", ex.ParamName);
            }

            [Fact]
            public void PayloadCannotBeNull()
            {
                var ex = Assert.Throws<ArgumentNullException>(() => new Message<Object>(Guid.NewGuid(), HeaderCollection.Empty, null));

                Assert.Equal("payload", ex.ParamName);
            }
        }

        public class WhenConvertingToString
        {
            [Fact]
            public void ReturnFriendlyDescription()
            {
                var id = Guid.NewGuid();
                var message = new Message<Object>(id, HeaderCollection.Empty, new Object());

                Assert.Equal($"{id} - System.Object", message.ToString());
            }
        }
    }
}
