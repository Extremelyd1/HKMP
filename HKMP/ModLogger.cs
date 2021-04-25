using Modding;

namespace HKMP {
    // Singleton class providing methods for logging purposes
    public class ModLogger : ILogger {
        public void Info(object origin, string message) {
            Log(LogLevel.Info, $"[{origin.GetType()}] {message}");
        }
        
        public void Fine(object origin, string message) {
            Log(LogLevel.Fine, $"[{origin.GetType()}] {message}");
        }
        
        public void Debug(object origin, string message) {
            Log(LogLevel.Debug, $"[{origin.GetType()}] {message}");
        }
        
        public void Warn(object origin, string message) {
            Log(LogLevel.Warn, $"[{origin.GetType()}] {message}");
        }
        
        public void Error(object origin, string message) {
            Log(LogLevel.Error, $"[{origin.GetType()}] {message}");
        }
        
        private void Log(LogLevel level, string message) {
            Modding.Logger.Log(message, level);
        }

    }
}