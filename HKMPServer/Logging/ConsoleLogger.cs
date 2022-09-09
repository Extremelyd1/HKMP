using Hkmp.Logging;
using HkmpServer.Command;

namespace HkmpServer.Logging {
    /// <summary>
    /// Logger implementation to log to console.
    /// </summary>
    internal class ConsoleLogger : BaseLogger {
        /// <summary>
        /// The console input manager for managing console input while writing output.
        /// </summary>
        private readonly ConsoleInputManager _consoleInputManager;

        public ConsoleLogger(ConsoleInputManager consoleInputManager) {
            _consoleInputManager = consoleInputManager;
        }

        /// <inheritdoc />
        public override void Info(string message) {
            _consoleInputManager.WriteLine($"[INFO] [{GetOriginClassName()}] {message}");
        }

        /// <inheritdoc />
        public override void Fine(string message) {
            _consoleInputManager.WriteLine($"[FINE] [{GetOriginClassName()}] {message}");
        }

        /// <inheritdoc />
        public override void Debug(string message) {
            _consoleInputManager.WriteLine($"[DEBUG] [{GetOriginClassName()}] {message}");
        }

        /// <inheritdoc />
        public override void Warn(string message) {
            _consoleInputManager.WriteLine($"[WARN] [{GetOriginClassName()}] {message}");
        }

        /// <inheritdoc />
        public override void Error(string message) {
            _consoleInputManager.WriteLine($"[ERROR] [{GetOriginClassName()}] {message}");
        }
    }
}
