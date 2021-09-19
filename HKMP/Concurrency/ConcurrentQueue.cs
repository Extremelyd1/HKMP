using System.Collections.Generic;

namespace Hkmp.Concurrency {
    public class ConcurrentQueue<T> {
        protected readonly object _lock = new object();
        protected readonly Queue<T> _queue = new Queue<T>();

        public void Enqueue(T value) {
            lock (_lock) {
                _queue.Enqueue(value);
            }
        }

        public T Dequeue() {
            lock (_lock) {
                return _queue.Dequeue();
            }
        }

        public void Clear() {
            lock (_lock) {
                _queue.Clear();
            }
        }

        public Queue<T> GetCopy() {
            lock (_lock) {
                return new Queue<T>(_queue);
            }
        }
    }
}