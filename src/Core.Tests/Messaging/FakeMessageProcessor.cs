using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Spark.Messaging;
using Spark.Threading;

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
    internal sealed class FakeMessageProcessor<T> : IProcessMessages<T>
    {
        private readonly TaskScheduler inlineTaskScheduler = new InlineTaskScheduler();
        private Exception nextExceptionToThrow ;
             
        private EventWaitHandle Continue { get; } = new AutoResetEvent(initialState: false);
        private EventWaitHandle Message { get; } = new AutoResetEvent(initialState: false);

        public Task ProcessAsync(Message<T> message)
        {
            return Task.Factory.StartNew(() => Process(message), CancellationToken.None, TaskCreationOptions.AttachedToParent, inlineTaskScheduler);
        }

        public void Process(Message<T> message)
        {
            Message.Set();
            Continue.WaitOne();

            if (nextExceptionToThrow != null)
            {
                var ex = nextExceptionToThrow;

                nextExceptionToThrow = null;

                throw ex;
            }
        }

        public void WaitForMessage() 
        {
            Message.WaitOne();
        }
        
        public void ProcessNextMessage()
        {
            Continue.Set();
        }

        public void ThrowException(Exception ex)
        {
            nextExceptionToThrow = ex;
            Continue.Set();
        }
    }
}
