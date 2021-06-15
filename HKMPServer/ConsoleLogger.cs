using System;
using HKMP;

namespace HKMPServer {
    public class ConsoleLogger : ILogger {
        
        private static string GetOriginString(object origin) {
            if (origin is string s) {
                return s;
            }

            return origin.GetType().ToString();
        }

        public void Info(object origin, string message) {
            Console.WriteLine($"[INFO] [{GetOriginString(origin)}] {message}");
        }

        public void Fine(object origin, string message) {
            Console.WriteLine($"[FINE] [{GetOriginString(origin)}] {message}");
        }

        public void Debug(object origin, string message) {
            Console.WriteLine($"[DEBUG] [{GetOriginString(origin)}] {message}");
        }

        public void Warn(object origin, string message) {
            Console.WriteLine($"[WARN] [{GetOriginString(origin)}] {message}");
        }

        public void Error(object origin, string message) {
            Console.WriteLine($"[ERROR] [{GetOriginString(origin)}] {message}");
        }
    }
}