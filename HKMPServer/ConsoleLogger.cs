using System;
using Hkmp;

namespace HkmpServer {
    /// <summary>
    /// Logger implementation to log to console.
    /// </summary>
    internal class ConsoleLogger : ILogger {
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
            Console.WriteLine($"[INFO] [{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Fine(object origin, string message) {
            Console.WriteLine($"[FINE] [{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Debug(object origin, string message) {
            Console.WriteLine($"[DEBUG] [{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Warn(object origin, string message) {
            Console.WriteLine($"[WARN] [{GetOriginString(origin)}] {message}");
        }

        /// <inheritdoc />
        public void Error(object origin, string message) {
            Console.WriteLine($"[ERROR] [{GetOriginString(origin)}] {message}");
        }
    }
}