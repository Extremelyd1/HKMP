using System;
using HKMP;

namespace HKMPServer {
    public class ConsoleLogger : ILogger {
        public void Info(object origin, string message) {
            Console.WriteLine($"[INFO] [{origin.GetType()}] {message}");
        }

        public void Fine(object origin, string message) {
            Console.WriteLine($"[FINE] [{origin.GetType()}] {message}");
        }

        public void Debug(object origin, string message) {
            Console.WriteLine($"[DEBUG] [{origin.GetType()}] {message}");
        }

        public void Warn(object origin, string message) {
            Console.WriteLine($"[WARN] [{origin.GetType()}] {message}");
        }

        public void Error(object origin, string message) {
            Console.WriteLine($"[ERROR] [{origin.GetType()}] {message}");
        }
    }
}