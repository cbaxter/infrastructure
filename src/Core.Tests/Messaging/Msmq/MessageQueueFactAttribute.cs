using System;
using System.Linq;
using System.Messaging;
using System.ServiceProcess;
using Xunit;
using Connection = Spark.Messaging.Msmq.MessageQueue;

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

namespace Test.Spark.Messaging.Msmq
{
    /// <summary>
    /// Constant for MSMQ Queue name.
    /// </summary>
    internal static class TestMessageQueue
    {
        public const String Path = @".\private$\spark.infrastructure.tests";

        /// <summary>
        /// Creates a new <see cref="MessageQueue"/> connection.
        /// </summary>
        public static Connection Create()
        {
            return new Connection(Path, QueueAccessMode.SendAndReceive) { MessageReadPropertyFilter = new MessagePropertyFilter { Id = true, LookupId = true, Body = true } };
        }

        /// <summary>
        /// Creates a new <see cref="MessageQueue"/> connection.
        /// </summary>
        public static Connection Create(String subqueue)
        {
            return new Connection(Path + ';' + subqueue, QueueAccessMode.SendAndReceive) { MessageReadPropertyFilter = new MessagePropertyFilter { Id = true, LookupId = true, Body = true } };
        }

        /// <summary>
        /// Deletes an existing <see cref="MessageQueue"/> instanceif exists.
        /// </summary>
        public static void Purge()
        {
            using (var testQueue = Create())
                testQueue.Purge();

            using (var poisonQueue = Create("poison"))
                poisonQueue.Purge();

            using (var processingQueue = Create("processing"))
                processingQueue.Purge();
        }

        /// <summary>
        /// Deletes an existing <see cref="MessageQueue"/> instanceif exists.
        /// </summary>
        public static void Delete()
        {
            if (MessageQueue.Exists(Path))
                MessageQueue.Delete(Path);
        }
    }

    /// <summary>
    /// MSMQ Integration Test Fact Attribute.
    /// </summary>
    public sealed class MessageQueueFactAttribute : FactAttribute
    {
        private static readonly String SkipReason;

        /// <summary>
        /// Determine if the configured SQL-Server instance is available.
        /// </summary>
        static MessageQueueFactAttribute()
        {
            if (ServiceController.GetServices().Any(service => service.ServiceName == "MSMQ" && service.Status == ServiceControllerStatus.Running))
            {
                try
                {
                    if (!MessageQueue.Exists(TestMessageQueue.Path)) MessageQueue.Create(TestMessageQueue.Path);
                    MessageQueue.Delete(TestMessageQueue.Path);

                    SkipReason = null;
                }
                catch (Exception ex)
                {
                    SkipReason = ex.Message;
                }
            }
            else
            {
                SkipReason = "The MSMQ Windows Service has not been started.";
            }
        }

        /// <summary>
        /// Ensure Skip reason set if SQL-Server instance is not available.
        /// </summary>
        public MessageQueueFactAttribute()
        {
            Skip = SkipReason;
        }
    }
}
