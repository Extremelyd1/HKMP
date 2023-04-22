using System;
using System.Collections.Generic;
using Hkmp.Api.Command.Server;
using HkmpServer.Logging;

namespace HkmpServer.Command {
    /// <summary>
    /// Command to exit and shutdown the server.
    /// </summary>
    internal class LogCommand : IServerCommand {
        /// <inheritdoc />
        public string Trigger => "/log";

        /// <inheritdoc />
        public string[] Aliases => Array.Empty<string>();

        /// <inheritdoc />
        public bool AuthorizedOnly => true;

        /// <summary>
        /// The logger class for logging to console.
        /// </summary>
        private readonly ConsoleLogger _consoleLogger;

        /// <summary>
        /// Construct the log command with the given console logger.
        /// </summary>
        /// <param name="consoleLogger">The console logger.</param>
        public LogCommand(ConsoleLogger consoleLogger) {
            _consoleLogger = consoleLogger;
        }

        /// <inheritdoc />
        public void Execute(ICommandSender commandSender, string[] args) {
            if (args.Length < 2) {
                commandSender.SendMessage($"Usage: {Trigger} [log level(s)]");
                return;
            }

            var levels = new HashSet<ConsoleLogger.Level>();
            for (var i = 1; i < args.Length; i++) {
                var levelString = args[i];
                if (Enum.TryParse<ConsoleLogger.Level>(levelString, true, out var level)) {
                    levels.Add(level);
                } else {
                    commandSender.SendMessage($"Invalid log level: {levelString}, available options: " +
                                              $"{string.Join(", ", Enum.GetNames(typeof(ConsoleLogger.Level)))}");
                    return;
                }
            }
            
            _consoleLogger.LoggableLevels.Clear();
            foreach (var level in levels) {
                _consoleLogger.LoggableLevels.Add(level);
            }
            
            commandSender.SendMessage($"Set console logging to following levels: {string.Join(", ", levels)}");
        }
    }
}
