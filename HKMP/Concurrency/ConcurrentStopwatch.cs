using System.Diagnostics;

namespace Hkmp.Concurrency;

/// <summary>
/// Thread-safe implementation of a stopwatch.
/// </summary>
public class ConcurrentStopwatch {
    /// <summary>
    /// Object for locking asynchronous access.
    /// </summary>
    private readonly object _lock = new object();

    /// <summary>
    /// The underlying stopwatch.
    /// </summary>
    private readonly Stopwatch _stopwatch = new Stopwatch();

    /// <summary>
    /// Gets the total elapsed time measured by the current instance, in milliseconds.
    /// </summary>
    /// <returns>A read-only long integer representing the total number of milliseconds measured by
    /// the current instance.</returns>
    public long ElapsedMilliseconds {
        get {
            lock (_lock) {
                return _stopwatch.ElapsedMilliseconds;
            }
        }
    }

    /// <summary>
    /// Stops time interval measurement and resets the elapsed time to zero.
    /// </summary>
    public void Reset() {
        lock (_lock) {
            _stopwatch.Reset();
        }
    }

    /// <summary>
    /// Stops time interval measurement, resets the elapsed time to zero, and starts measuring elapsed time.
    /// </summary>
    public void Restart() {
        lock (_lock) {
            _stopwatch.Restart();
        }
    }
}
