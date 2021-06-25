using Modding;

namespace Hkmp {
    // Singleton class providing methods for logging purposes
    public class ModLogger : ILogger {
        private static string GetOriginString(object origin) {
            if (origin is string s) {
                return s;
            }

            return origin.GetType().ToString();
        }

        public void Info(object origin, string message) {
            Log(LogLevel.Info, $"[{GetOriginString(origin)}] {message}");
        }

        public void Fine(object origin, string message) {
            Log(LogLevel.Fine, $"[{GetOriginString(origin)}] {message}");
        }

        public void Debug(object origin, string message) {
            Log(LogLevel.Debug, $"[{GetOriginString(origin)}] {message}");
        }

        public void Warn(object origin, string message) {
            Log(LogLevel.Warn, $"[{GetOriginString(origin)}] {message}");
        }

        public void Error(object origin, string message) {
            Log(LogLevel.Error, $"[{GetOriginString(origin)}] {message}");
        }

        private void Log(LogLevel level, string message) {
            Modding.Logger.Log(message, level);
        }
    }
}