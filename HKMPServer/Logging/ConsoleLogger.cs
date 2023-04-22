using System.Collections.Generic;
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
        
        /// <summary>
        /// The log levels that will be logged to the console.
        /// </summary>
        public readonly HashSet<Level> LoggableLevels;

        public ConsoleLogger(ConsoleInputManager consoleInputManager) {
            _consoleInputManager = consoleInputManager;
            LoggableLevels = new HashSet<Level> {
                Level.Error,
                Level.Warn,
                Level.Info
            };
        }

        /// <inheritdoc />
        public override void Info(string message) {
            if (!LoggableLevels.Contains(Level.Info)) {
                return;
            }
            
#if DEBUG
            _consoleInputManager.WriteLine($"[INFO] [{GetOriginClassName()}] {message}");
#else
            _consoleInputManager.WriteLine($"[INFO] {message}");
#endif
        }

        /// <inheritdoc />
        public override void Fine(string message) {
            if (!LoggableLevels.Contains(Level.Fine)) {
                return;
            }
            
#if DEBUG
            _consoleInputManager.WriteLine($"[FINE] [{GetOriginClassName()}] {message}");
#else
            _consoleInputManager.WriteLine($"[FINE] {message}");
#endif
        }

        /// <inheritdoc />
        public override void Debug(string message) {
            if (!LoggableLevels.Contains(Level.Debug)) {
                return;
            }
            
#if DEBUG
            _consoleInputManager.WriteLine($"[DEBUG] [{GetOriginClassName()}] {message}");
#else
            _consoleInputManager.WriteLine($"[DEBUG] {message}");
#endif
        }

        /// <inheritdoc />
        public override void Warn(string message) {
            if (!LoggableLevels.Contains(Level.Warn)) {
                return;
            }
            
#if DEBUG
            _consoleInputManager.WriteLine($"[WARN] [{GetOriginClassName()}] {message}");
#else
            _consoleInputManager.WriteLine($"[WARN] {message}");
#endif
        }

        /// <inheritdoc />
        public override void Error(string message) {
            if (!LoggableLevels.Contains(Level.Error)) {
                return;
            }
            
#if DEBUG
            _consoleInputManager.WriteLine($"[ERROR] [{GetOriginClassName()}] {message}");
#else
            _consoleInputManager.WriteLine($"[ERROR] {message}");
#endif
        }

        public enum Level {
            Error,
            Warn,
            Info,
            Fine,
            Debug
        }
    }
}
