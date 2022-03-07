namespace Hkmp.Concurrency {
    public class ConcurrentFixedSizeQueue<T> : ConcurrentQueue<T> {
        private readonly int _size;

        public ConcurrentFixedSizeQueue(int size) {
            _size = size;
        }

        public new void Enqueue(T value) {
            lock (_lock) {
                _queue.Enqueue(value);

                while (_queue.Count > _size) {
                    _queue.Dequeue();
                }
            }
        }
    }
}