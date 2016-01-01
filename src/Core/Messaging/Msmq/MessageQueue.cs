using System;
using System.ComponentModel;
using System.Messaging;
using System.Runtime.InteropServices;
using Spark.Logging;

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

namespace Spark.Messaging.Msmq
{
    /// <summary>
    /// Extends <see cref="System.Messaging.MessageQueue"/> to add in support for moving messages between subqueues (only supported by native API).
    /// </summary>
    [DesignerCategory("")]
    internal sealed class MessageQueue : System.Messaging.MessageQueue
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();
        private IntPtr moveHandle = IntPtr.Zero;

        #region Native Methods

        /// <summary>
        /// Native method used to open a queue for sending, peeking at, retrieving, or purging messages.
        /// </summary>
        /// <param name="formatName">The format name string of the queue you want to open.</param>
        /// <param name="access">Specifies how the application accesses the queue (peek, send, or receive).</param>
        /// <param name="shareMode">How the queue will be shared.</param>
        /// <param name="hQueue">Pointer to a handle to the opened queue.</param>
        [DllImport("mqrt.dll", EntryPoint = "MQOpenQueue", CharSet = CharSet.Unicode)]
        private static extern int OpenQueue(String formatName, Int32 access, Int32 shareMode, ref IntPtr hQueue);

        /// <summary>
        /// Moves messages between a queue and its subqueue, or between two subqueues within the same main queue.
        /// </summary>
        /// <param name="sourceQueue">A handle of the queue that the message has to be moved from.</param>
        /// <param name="targetQueue">A handle of the subqueue to which the message is to be moved.</param>
        /// <param name="lookupID">The identity of the message that needs to be moved.</param>
        /// <param name="pTransaction">A pointer to an ITransaction interface, a constant, or NULL.</param>
        [DllImport("mqrt.dll", EntryPoint = "MQMoveMessage", CharSet = CharSet.Unicode)]
        private static extern int MoveMessage(IntPtr sourceQueue, IntPtr targetQueue, Int64 lookupID, IntPtr pTransaction);

        /// <summary>
        /// Native method used to closes a given queue or subqueue.
        /// </summary>
        /// <param name="queue">A handle of the queue that be closed.</param>
        [DllImport("mqrt.dll", EntryPoint = "MQCloseQueue", CharSet = CharSet.Unicode)]
        private static extern int CloseQueue(IntPtr queue);

        #endregion

        /// <summary>
        /// Gets or sets the underlying message queue write handle that will be used when moving messages between subqueues.
        /// </summary>
        private IntPtr MoveHandle
        {
            get
            {
                if (moveHandle == IntPtr.Zero) moveHandle = OpenQueue(FormatName);
                return moveHandle;
            }
        }

        /// <summary>
        /// Initialize static members of <see cref="MessageQueue"/>.
        /// </summary>
        static MessageQueue()
        {
            if (EnableConnectionCache) return;
            Log.Warn("MSMQ connection cache disabled; message queue performance will be degraded. Consider setting --> MessageQueue.EnableConnectionCache = true;");
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MessageQueue"/>.
        /// </summary>
        /// <param name="path">The location of the queue referenced by this <see cref="MessageQueue"/>, which can be "<value>.</value>" for the local computer.</param>
        /// <param name="accessMode">One of the <see cref="QueueAccessMode"/> values.</param>
        public MessageQueue(String path, QueueAccessMode accessMode)
            : base(path, sharedModeDenyReceive: false, enableCache: true, accessMode: accessMode)
        { }

        /// <summary>
        /// Disposes of the resources (other than memory) ysed by the <see cref="MessageQueue"/>.
        /// </summary>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            if (moveHandle != IntPtr.Zero)
            {
                CloseQueue(moveHandle);
                moveHandle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Returns <value>true</value> if the message queue already exists or was created successfully; otherwise returns <value>false</value>.
        /// </summary>
        public Boolean EnsureQueueExists()
        {
            return InitializeMessageQueue(Path);
        }

        /// <summary>
        /// Attempts to initialize the message queue if the <paramref name="path"/> does not already exist.
        /// </summary>
        /// <param name="path">The queue path.</param>
        public static Boolean InitializeMessageQueue(String path)
        {
            try
            {
                path = path.Substring(path.LastIndexOf(':') + 1);
                if (path.IndexOf(';') > 0) path = path.Substring(0, path.IndexOf(';'));
                if (!Exists(path)) Create(path, transactional: false);

                return true;
            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);

                return false;
            }
        }

        /// <summary>
        /// Opens a queue with <value>MQ_MOVE_ACCESS</value> that is not suppored by the managed <see cref="System.Messaging.MessageQueue"/> class.
        /// </summary>
        /// <param name="formatName">The unique queue name that identifies the message queue to open.</param>
        private static IntPtr OpenQueue(String formatName)
        {
            var handle = IntPtr.Zero;
            var error = OpenQueue(formatName, 4 /* MQ_MOVE_ACCESS */, 0 /* MQ_DENY_NONE */, ref handle);

            if (error < 0) throw new Win32Exception(error);

            return handle;
        }

        /// <summary>
        /// Moves a message from the underlying message queue to the <paramref name="target"/> message queue.
        /// </summary>
        /// <param name="message">The message to be moved.</param>
        /// <param name="target">The target subqueue where the <paramref name="message"/> should be moved.</param>
        public void Move(System.Messaging.Message message, MessageQueue target)
        {
            var error = MoveMessage(ReadHandle, target.MoveHandle, message.LookupId, IntPtr.Zero);
            if (error < 0) throw new Win32Exception(error);
        }
    }
}
