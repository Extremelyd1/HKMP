using System.Diagnostics;

namespace Hkmp.Concurrency {
    public class ConcurrentStopwatch {
        private readonly object _lock = new object();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public long ElapsedMilliseconds {
            get {
                lock (_lock) {
                    return _stopwatch.ElapsedMilliseconds;
                }
            }
        }

        public void Reset() {
            lock (_lock) {
                _stopwatch.Reset();
            }
        }

        public void Restart() {
            lock (_lock) {
                _stopwatch.Restart();
            }
        }
    }
}