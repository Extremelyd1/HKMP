using System.Collections.Generic;

namespace HKMP.Concurrency {
    public class ConcurrentDictionary<TKey, TValue> {
        
        private readonly object _lock = new object();
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public TValue this[TKey key] {
            get {
                lock (_lock) {
                    return _dictionary[key];
                }
            }
            set {
                lock (_lock) {
                    _dictionary[key] = value;
                }
            }
        }

        public void Remove(TKey key) {
            lock (_lock) {
                _dictionary.Remove(key);
            }
        }

        public bool TryGetValue(TKey key, out TValue value) {
            lock (_lock) {
                return _dictionary.TryGetValue(key, out value);
            }
        }

        public void Clear() {
            lock (_lock) {
                _dictionary.Clear();
            }
        }

        public Dictionary<TKey, TValue> GetCopy() {
            lock (_lock) {
                return new Dictionary<TKey, TValue>(_dictionary);
            }
        }
    }
}