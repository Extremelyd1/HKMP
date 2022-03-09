namespace Hkmp.Concurrency {
    /// <summary>
    /// Thread-safe implementation of a fixed-size queue.
    /// </summary>
    /// <typeparam name="T">The type of the values in the queue.</typeparam>
    public class ConcurrentFixedSizeQueue<T> : ConcurrentQueue<T> {
        /// <summary>
        /// The size of the queue.
        /// </summary>
        private readonly int _size;

        /// <summary>
        /// Constructs the fixed-size queue with the given size.
        /// </summary>
        /// <param name="size">The size of the queue.</param>
        public ConcurrentFixedSizeQueue(int size) {
            _size = size;
        }

        /// <summary>
        /// Adds an object to the end of the queue. Will dequeue items until the queue is the
        /// correct size again.
        /// </summary>
        /// <param name="value">The object to add to the queue. The value can be <code>null</code> for reference
        /// types.</param>
        public new void Enqueue(T value) {
            lock (Lock) {
                Queue.Enqueue(value);

                while (Queue.Count > _size) {
                    Queue.Dequeue();
                }
            }
        }
    }
}