using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SagaTimeoutNode = System.Collections.Generic.LinkedListNode<Spark.Cqrs.Eventing.Sagas.SagaTimeout>;

namespace Spark.Cqrs.Eventing.Sagas
{
    /// <summary>
    /// Represented a collection of sorted saga timeouts.
    /// </summary>
    internal sealed class SagaTimeoutCollection : ICollection<SagaTimeout>
    {
        private readonly Dictionary<SagaReference, List<SagaTimeoutNode>> scheduledSagaTimeouts = new Dictionary<SagaReference, List<SagaTimeoutNode>>();
        private readonly LinkedList<SagaTimeout> sortedSagaTimeouts = new LinkedList<SagaTimeout>();

        /// <summary>
        /// Gets the number of timeouts contained within the <see cref="SagaTimeoutCollection"/>.
        /// </summary>
        public Int32 Count { get { return sortedSagaTimeouts.Count; } }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        Boolean ICollection<SagaTimeout>.IsReadOnly { get { return false; } }

        /// <summary>
        /// Adds a <see cref="SagaTimeout"/> to the sorted collection.
        /// </summary>
        /// <param name="sagaTimeout"></param>
        public void Add(SagaTimeout sagaTimeout)
        {
            SagaTimeoutNode node = sortedSagaTimeouts.First;
            SagaReference sagaReference = sagaTimeout;
            List<SagaTimeoutNode> timeouts;

            if (!scheduledSagaTimeouts.TryGetValue(sagaReference, out timeouts))
                scheduledSagaTimeouts[sagaReference] = timeouts = new List<SagaTimeoutNode>();

            while (node != null && node.Value.Timeout <= sagaTimeout.Timeout)
                node = node.Next;

            timeouts.Add(node == null ? sortedSagaTimeouts.AddLast(sagaTimeout) : sortedSagaTimeouts.AddBefore(node, sagaTimeout));
        }

        /// <summary>
        /// Removes all saga timeouts from the <see cref="SagaTimeoutCollection"/>.
        /// </summary>
        public void Clear()
        {
            scheduledSagaTimeouts.Clear();
            sortedSagaTimeouts.Clear();
        }

        /// <summary>
        /// Determines whether a <see cref="SagaReference"/> has one or more saga timeouts in the <see cref="SagaTimeoutCollection"/>.
        /// </summary>
        /// <param name="sagaReference">The <see cref="SagaReference"/> to locate in the <see cref="SagaTimeoutCollection"/>.</param>
        public Boolean Contains(SagaReference sagaReference)
        {
            return scheduledSagaTimeouts.ContainsKey(sagaReference);
        }

        /// <summary>
        /// Determines whether a <see cref="SagaTimeout"/> is in the <see cref="SagaTimeoutCollection"/>.
        /// </summary>
        /// <param name="sagaTimeout">The <see cref="SagaTimeout"/> to locate in the <see cref="SagaTimeoutCollection"/>.</param>
        public Boolean Contains(SagaTimeout sagaTimeout)
        {
            List<SagaTimeoutNode> sagaTimeouts;
            return scheduledSagaTimeouts.TryGetValue(sagaTimeout, out sagaTimeouts) && sagaTimeouts.Any(timeout => timeout.Value.Equals(sagaTimeout));
        }

        /// <summary>
        /// Copies the entire <see cref="SagaTimeoutCollection"/> to a compatible one dimensional <see cref="Array"/> starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="SagaTimeoutCollection"/>. The <see cref="Array"/> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(SagaTimeout[] array, Int32 index)
        {
            sortedSagaTimeouts.CopyTo(array, index);
        }

        /// <summary>
        /// Removes all saga timeouts associated with the specified <paramref name="sagaReference"/>.
        /// </summary>
        /// <param name="sagaReference">The saga reference for which all saga timeouts are to be removed.</param>
        public Boolean Remove(SagaReference sagaReference)
        {
            List<SagaTimeoutNode> sagaTimeouts;
            if (!scheduledSagaTimeouts.TryGetValue(sagaReference, out sagaTimeouts))
                return false;

            scheduledSagaTimeouts.Remove(sagaReference);
            foreach (var sagaTimeout in sagaTimeouts)
                sortedSagaTimeouts.Remove(sagaTimeout);

            return sagaTimeouts.Count > 0;
        }

        public Boolean Remove(SagaTimeout sagaTimeout)
        {
            return Remove(sagaTimeout, removeAll: false);
        }

        public Boolean RemoveAll(SagaTimeout sagaTimeout)
        {
            return Remove(sagaTimeout, removeAll: true);
        }

        private Boolean Remove(SagaTimeout sagaTimeout, Boolean removeAll)
        {
            Boolean result = false;
            List<SagaTimeoutNode> sagaTimeouts;
            SagaReference sagaReference = sagaTimeout;

            if (!scheduledSagaTimeouts.TryGetValue(sagaReference, out sagaTimeouts))
                return false;

            for (var i = sagaTimeouts.Count - 1; i >= 0; i--)
            {
                var node = sagaTimeouts[i];
                if (!sagaTimeout.Equals(node.Value))
                    continue;

                sortedSagaTimeouts.Remove(node);
                sagaTimeouts.RemoveAt(i);
                result = true;

                if (!removeAll) break;
            }

            if (sagaTimeouts.Count == 0)
                scheduledSagaTimeouts.Remove(sagaReference);

            return result;
        }

        public IEnumerator<SagaTimeout> GetEnumerator()
        {
            return sortedSagaTimeouts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
