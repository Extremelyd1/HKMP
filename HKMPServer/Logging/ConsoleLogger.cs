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

        public override void Info(string message) {
            _consoleInputManager.WriteLine($"[INFO] [{GetOriginClassName()}] {message}");
        }
        
        public override void Fine(string message) {
            _consoleInputManager.WriteLine($"[FINE] [{GetOriginClassName()}] {message}");
        }
        
        public override void Debug(string message) {
            _consoleInputManager.WriteLine($"[DEBUG] [{GetOriginClassName()}] {message}");
        }
        
        public override void Warn(string message) {
            _consoleInputManager.WriteLine($"[WARN] [{GetOriginClassName()}] {message}");
        }
        
        public override void Error(string message) {
            _consoleInputManager.WriteLine($"[ERROR] [{GetOriginClassName()}] {message}");
        }
    }
}