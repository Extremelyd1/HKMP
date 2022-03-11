using Modding;

namespace Hkmp {
    /// <summary>
    /// Logger class for logging to the ModLog.txt file.
    /// </summary>
    internal class ModLogger : ILogger {
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

        /// <inheritdoc />
        public void Info(object origin, string message) {
            Log(LogLevel.Info, $"[{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Fine(object origin, string message) {
            Log(LogLevel.Fine, $"[{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Debug(object origin, string message) {
            Log(LogLevel.Debug, $"[{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Warn(object origin, string message) {
            Log(LogLevel.Warn, $"[{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Error(object origin, string message) {
            Log(LogLevel.Error, $"[{GetOriginString(origin)}] {message}");
        }

        /// <summary>
        /// Log the given message with the given log level to the ModLog.
        /// </summary>
        /// <param name="level">The log level of the message.</param>
        /// <param name="message">The message to log.</param>
        private static void Log(LogLevel level, string message) {
            Modding.Logger.Log(message, level);
        }
    }
}