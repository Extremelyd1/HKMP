using System.Collections.Generic;

namespace Hkmp.Concurrency;

/// <summary>
/// Thread-safe implementation of a fixed-size queue.
/// </summary>
/// <typeparam name="T">The type of the values in the queue.</typeparam>
public class ConcurrentFixedSizeQueue<T> {
    /// <summary>
    /// Object for locking asynchronous access.
    /// </summary>
    private readonly object _lock = new object();

    /// <summary>
    /// The underlying queue.
    /// </summary>
    private readonly Queue<T> _queue = new Queue<T>();

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
    public void Enqueue(T value) {
        lock (_lock) {
            _queue.Enqueue(value);

            while (_queue.Count > _size) {
                _queue.Dequeue();
            }
        }
    }

    /// <summary>
    /// Removes and returns the object at the beginning of the queue.
    /// </summary>
    /// <returns>The object that is removed from the beginning of the queue.</returns>
    public T Dequeue() {
        lock (_lock) {
            return _queue.Dequeue();
        }
    }

    /// <summary>
    /// Removes all objects from the queue.
    /// </summary>
    public void Clear() {
        lock (_lock) {
            _queue.Clear();
        }
    }

    /// <summary>
    /// Get a copy of the underlying queue for synchronous access.
    /// </summary>
    /// <returns>A shallow copy of the queue.</returns>
    public Queue<T> GetCopy() {
        lock (_lock) {
            return new Queue<T>(_queue);
        }
    }
}
