namespace Hkmp {
    internal static class Logger {
        /// <summary>
        /// The logger instance.
        /// </summary>
        private static ILogger _logger;

        /// <summary>
        /// Get the logger instance.
        /// </summary>
        /// <returns>An instance of ILogger.</returns>
        public static ILogger Get() {
            return _logger;
        }

        /// <summary>
        /// Set the logger instance to use.
        /// </summary>
        /// <param name="logger">The instance of ILogger.</param>
        public static void SetLogger(ILogger logger) {
            _logger = logger;
        }
    }
}