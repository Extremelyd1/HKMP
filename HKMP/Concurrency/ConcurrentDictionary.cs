using System.Collections.Generic;

namespace Hkmp.Concurrency {
    /// <summary>
    /// Thread-safe (limited) implementation of a dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public class ConcurrentDictionary<TKey, TValue> {
        /// <summary>
        /// Object for locking asynchronous access.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The underlying dictionary.
        /// </summary>
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
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

        /// <summary>
        /// Removes the value with the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        public void Remove(TKey key) {
            lock (_lock) {
                _dictionary.Remove(key);
            }
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key,
        /// if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter.
        /// This parameter is passed uninitialized.</param>
        /// <returns>true if the dictionary contains an element with the specified key; otherwise
        /// false</returns>
        public bool TryGetValue(TKey key, out TValue value) {
            lock (_lock) {
                return _dictionary.TryGetValue(key, out value);
            }
        }

        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public void Clear() {
            lock (_lock) {
                _dictionary.Clear();
            }
        }

        /// <summary>
        /// Get a copy of the underlying dictionary for synchronous access.
        /// </summary>
        /// <returns>A shallow copy of the dictionary.</returns>
        public Dictionary<TKey, TValue> GetCopy() {
            lock (_lock) {
                return new Dictionary<TKey, TValue>(_dictionary);
            }
        }
    }
}
