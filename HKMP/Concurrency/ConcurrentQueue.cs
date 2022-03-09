using System.Collections.Generic;

namespace Hkmp.Concurrency {
    /// <summary>
    /// Thread-safe implementation of a queue.
    /// </summary>
    /// <typeparam name="T">The type of the values in the queue.</typeparam>
    public class ConcurrentQueue<T> {
        /// <summary>
        /// Object for locking asynchronous access.
        /// </summary>
        protected readonly object Lock = new object();
        /// <summary>
        /// The underlying queue.
        /// </summary>
        protected readonly Queue<T> Queue = new Queue<T>();

        /// <summary>
        /// Adds an object to the end of the queue.
        /// </summary>
        /// <param name="value">The object to add to the queue. The value can be <code>null</code> for reference
        /// types.</param>
        public void Enqueue(T value) {
            lock (Lock) {
                Queue.Enqueue(value);
            }
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the queue.
        /// </summary>
        /// <returns>The object that is removed from the beginning of the queue.</returns>
        public T Dequeue() {
            lock (Lock) {
                return Queue.Dequeue();
            }
        }

        /// <summary>
        /// Removes all objects from the queue.
        /// </summary>
        public void Clear() {
            lock (Lock) {
                Queue.Clear();
            }
        }

        /// <summary>
        /// Get a copy of the underlying queue for synchronous access.
        /// </summary>
        /// <returns>A shallow copy of the queue.</returns>
        public Queue<T> GetCopy() {
            lock (Lock) {
                return new Queue<T>(Queue);
            }
        }
    }
}