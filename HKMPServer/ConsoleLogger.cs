using Hkmp;
using HkmpServer.Command;

namespace HkmpServer {
    /// <summary>
    /// Logger implementation to log to console.
    /// </summary>
    internal class ConsoleLogger : ILogger {
        /// <summary>
        /// The console input manager for managing console input while writing output.
        /// </summary>
        private readonly ConsoleInputManager _consoleInputManager;

        public ConsoleLogger(ConsoleInputManager consoleInputManager) {
            _consoleInputManager = consoleInputManager;
        }

        /// <inheritdoc />
        public void Info(object origin, string message) {
            _consoleInputManager.WriteLine($"[INFO] [{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Fine(object origin, string message) {
            _consoleInputManager.WriteLine($"[FINE] [{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Debug(object origin, string message) {
            _consoleInputManager.WriteLine($"[DEBUG] [{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Warn(object origin, string message) {
            _consoleInputManager.WriteLine($"[WARN] [{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Error(object origin, string message) {
            _consoleInputManager.WriteLine($"[ERROR] [{GetOriginString(origin)}] {message}");
        }
        
        /// <summary>
        /// Get the origin object as a string. If the origin parameter is already a string, return that. Otherwise,
        /// we return the type of the origin object as a string.
        /// </summary>
        /// <param name="origin">The origin object.</param>
        /// <returns>A string of the origin object.</returns>
        private static string GetOriginString(object origin) {
            if (origin is string s) {
                return s;
            }

            return origin.GetType().ToString();
        }
    }
}