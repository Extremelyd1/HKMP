namespace HKMP {
    public class Logger {
        private static ILogger _logger;

        public static ILogger Get() {
            return _logger;
        }

        public static void SetLogger(ILogger logger) {
            _logger = logger;
        }
    }
}