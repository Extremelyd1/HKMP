using System.Collections.Generic;

namespace Hkmp.Concurrency {
    public class ConcurrentList<T> {
        private readonly object _lock = new object();
        private readonly List<T> _list = new List<T>();

        public T this[int index] {
            get {
                lock (_lock) {
                    return _list[index];
                }
            }
            set {
                lock (_lock) {
                    _list[index] = value;
                }
            }
        }
        
        public void Add(T item) {
            lock (_lock) {
                _list.Add(item);
            }
        }

        public bool Remove(T item) {
            lock (_lock) {
                return _list.Remove(item);
            }
        }

        public void Clear() {
            lock (_lock) {
                _list.Clear();
            }
        }

        public List<T> GetCopy() {
            lock (_lock) {
                return new List<T>(_list);
            }
        }
    }
}