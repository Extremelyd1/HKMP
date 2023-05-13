using System.Collections.Generic;

namespace Hkmp.Concurrency;

/// <summary>
/// Thread-safe implementation of a list.
/// </summary>
/// <typeparam name="T">The type of the values in the list.</typeparam>
public class ConcurrentList<T> {
    /// <summary>
    /// Object for locking asynchronous access.
    /// </summary>
    private readonly object _lock = new object();

    /// <summary>
    /// The underlying list.
    /// </summary>
    private readonly List<T> _list = new List<T>();

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="index">The index of the value to get or set.</param>
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

    /// <summary>
    /// Add the given item to the list.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item) {
        lock (_lock) {
            _list.Add(item);
        }
    }

    /// <summary>
    /// Removes the given item from the list.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>true if the item was removed, false if no such item could be found.
    /// </returns>
    public bool Remove(T item) {
        lock (_lock) {
            return _list.Remove(item);
        }
    }

    /// <summary>
    /// Removes all items from the list.
    /// </summary>
    public void Clear() {
        lock (_lock) {
            _list.Clear();
        }
    }

    /// <summary>
    /// Get a copy of the underlying list for synchronous access.
    /// </summary>
    /// <returns>A shallow copy of the dictionary.</returns>
    public List<T> GetCopy() {
        lock (_lock) {
            return new List<T>(_list);
        }
    }
}
