using Modding;

namespace HKMP {
    // Logger class providing static functions for logging purposes
    public static class Logger {
        public static void Info(object origin, string message) {
            Log(LogLevel.Info, $"[{origin.GetType()}] {message}");
        }
        
        public static void Fine(object origin, string message) {
            Log(LogLevel.Fine, $"[{origin.GetType()}] {message}");
        }
        
        public static void Debug(object origin, string message) {
            Log(LogLevel.Debug, $"[{origin.GetType()}] {message}");
        }
        
        public static void Warn(object origin, string message) {
            Log(LogLevel.Warn, $"[{origin.GetType()}] {message}");
        }
        
        public static void Error(object origin, string message) {
            Log(LogLevel.Error, $"[{origin.GetType()}] {message}");
        }
        
        private static void Log(LogLevel level, string message) {
            Modding.Logger.Log(message, level);
        }

    }
}