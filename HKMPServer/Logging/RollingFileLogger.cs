using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Hkmp.Logging;

namespace HkmpServer.Logging {
    /// <summary>
    /// Logger implementation for logging to rolling files.
    /// </summary>
    internal class RollingFileLogger : BaseLogger {
        /// <summary>
        /// The name of the directory for log files.
        /// </summary>
        private const string LogFileDirectory = "logs";

        /// <summary>
        /// The base name (without extension) of the current log file.
        /// </summary>
        private const string LogFileName = "server";

        /// <summary>
        /// The extension of the log files.
        /// </summary>
        private const string LogFileExtension = ".log";

        /// <summary>
        /// The wildcard character.
        /// </summary>
        private const string FileWildcard = "*";

        /// <summary>
        /// The maximum size a log file can have before being rolled.
        /// </summary>
        private const int MaxLogSize = 1024 * 1024 * 100;

        /// <summary>
        /// The maximum number of old rolled log files to keep.
        /// </summary>
        private const int MaxLogFiles = 10;

        /// <summary>
        /// The full path of the current log file.
        /// </summary>
        private readonly string _logFile;

        /// <summary>
        /// The full path of the log directory.
        /// </summary>
        private readonly string _logDirectory;

        /// <summary>
        /// The log file name with wildcard for rolled log files.
        /// </summary>
        private readonly string _logFileWildcard;

        /// <summary>
        /// Lock object to prevent concurrent access.
        /// </summary>
        private readonly object _logLock = new object();

        /// <summary>
        /// The current stream writer to write to the log file.
        /// </summary>
        private StreamWriter _currentWriter;

        /// <summary>
        /// Whether there is a stream writer for the current log file.
        /// </summary>
        private bool _hasWriter;

        public RollingFileLogger() {
            // We first try to get the entry assembly in case the executing assembly was
            // embedded in the standalone server
            var assembly = Assembly.GetEntryAssembly();
            if (assembly == null) {
                // If the entry assembly doesn't exist, we fall back on the executing assembly
                assembly = Assembly.GetExecutingAssembly();
            }

            var currentPath = Path.GetDirectoryName(assembly.Location);
            if (currentPath == null) {
                throw new Exception("Could not get directory of assembly for logging");
            }

            // Store commonly used strings
            _logFile = Path.Combine(currentPath, LogFileDirectory, LogFileName + LogFileExtension);
            _logDirectory = Path.GetDirectoryName(_logFile) + Path.DirectorySeparatorChar;
            _logFileWildcard = LogFileName + FileWildcard + LogFileExtension;
        }

        /// <summary>
        /// Log a given message to the current file. Also roll the current file if it exceeds file size.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogMessage(string message) {
            lock (_logLock) {
                try {
                    if (!_hasWriter) {
                        if (!File.Exists(_logFile)) {
                            Directory.CreateDirectory(_logDirectory);

                            _currentWriter = new StreamWriter(File.Create(_logFile));
                        } else {
                            _currentWriter = new StreamWriter(_logFile, true);
                        }

                        _hasWriter = true;
                    }

                    _currentWriter.WriteLine(message);
                    _currentWriter.Flush();

                    RollLogFile();
                } catch (Exception e) {
                    // Can't really log this error to file, since that is what went wrong in the first place
                    // So we log to debug in case we have a debug build
                    System.Diagnostics.Debug.WriteLine(
                        $"Exception occurred while writing to log files:\n{e}");
                }
            }
        }

        /// <summary>
        /// Roll the current log file and other saved log files accordingly.
        /// </summary>
        private void RollLogFile() {
            var length = new FileInfo(_logFile).Length;

            if (length > MaxLogSize) {
                var logFiles = Directory.GetFiles(
                    _logDirectory,
                    _logFileWildcard,
                    SearchOption.TopDirectoryOnly
                ).ToList();

                if (logFiles.Count > 0) {
                    // Filter out the current log file
                    logFiles = logFiles.Where(
                        fileName => !fileName.EndsWith(LogFileName + LogFileExtension)
                    ).ToList();
                    // Sort the log files that have indices
                    logFiles.Sort();

                    // If we have now reached the maximum number of possible log files, delete the oldest
                    if (logFiles.Count >= MaxLogFiles) {
                        File.Delete(logFiles[MaxLogFiles - 1]);

                        logFiles.RemoveAt(MaxLogFiles - 1);
                    }

                    // Move all the files from oldest to newest
                    for (var i = logFiles.Count; i > 0; i--) {
                        File.Move(logFiles[i - 1], _logDirectory + LogFileName + "." + i + LogFileExtension);
                    }

                    // Move the original file as well
                    File.Move(_logFile, _logDirectory + LogFileName + ".0" + LogFileExtension);

                    _currentWriter.Dispose();
                    _hasWriter = false;
                }
            }
        }

        /// <inheritdoc />
        public override void Info(string message) {
#if DEBUG
            LogMessage($"[INFO] [{GetOriginClassName()}] {message}");
#else
            LogMessage($"[INFO] {message}");
#endif
        }

        /// <inheritdoc />
        public override void Fine(string message) {
#if DEBUG
            LogMessage($"[FINE] [{GetOriginClassName()}] {message}");
#else
            LogMessage($"[FINE] {message}");
#endif
        }

        /// <inheritdoc />
        public override void Debug(string message) {
#if DEBUG
            LogMessage($"[DEBUG] [{GetOriginClassName()}] {message}");
#else
            LogMessage($"[DEBUG] {message}");
#endif
        }

        /// <inheritdoc />
        public override void Warn(string message) {
#if DEBUG
            LogMessage($"[WARN] [{GetOriginClassName()}] {message}");
#else
            LogMessage($"[WARN] {message}");
#endif
        }

        /// <inheritdoc />
        public override void Error(string message) {
#if DEBUG
            LogMessage($"[ERROR] [{GetOriginClassName()}] {message}");
#else
            LogMessage($"[ERROR] {message}");
#endif
        }
    }
}
