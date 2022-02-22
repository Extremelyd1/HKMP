using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Hkmp.Game.Command {
    /// <summary>
    /// Class that manages commands for both client and server side.
    /// </summary>
    public class CommandManager {
        /// <summary>
        /// Dictionary mapping command triggers and aliases to their respective commands.
        /// </summary>
        private readonly Dictionary<string, Command> _commands;

        public CommandManager() {
            _commands = new Dictionary<string, Command>();
        }

        /// <summary>
        /// Try to process a command given the message.
        /// </summary>
        /// <param name="message">A user-input string message.</param>
        /// <returns>True if the message was processed as a command, false otherwise.</returns>
        public bool ProcessCommand(string message) {
            var argList = new List<string>();

            var argRegex = new Regex("([^\"\\s]\\S*|\".*?\")");
            var matches = argRegex.Matches(message);

            foreach (var match in matches) {
                argList.Add(match.ToString().Replace("\"", ""));
            }

            if (argList.Count == 0) {
                return false;
            }

            var commandString = argList[0];
            if (_commands.TryGetValue(commandString, out var command)) {
                command.Execute(argList.ToArray());

                return true;
            }

            return false;
        }

        /// <summary>
        /// Register a given command.
        /// </summary>
        /// <param name="command">The command implementation.</param>
        /// <returns>True if the command trigger was successfully registered, false otherwise.
        /// Aliases may or may not be registered depending on whether they are available.</returns>
        public bool RegisterCommand(Command command) {
            // Check if the trigger for this command already exists and if so, we return false
            if (_commands.ContainsKey(command.Trigger)) {
                Logger.Get().Warn(this,$"Could not register command: {command.Trigger}, another command under that trigger was already registered");
                return false;
            }

            _commands[command.Trigger] = command;

            // For each of the aliases we check if it exists and if so, we skip it
            // Aliases are not necessary for correct functioning
            foreach (var alias in command.Aliases) {
                if (_commands.ContainsKey(alias)) {
                    Logger.Get().Warn(this, $"Could not register command alias: {command.Aliases} for command: {command.Trigger}, the alias was already registered");
                    continue;
                }

                _commands[alias] = command;
            }

            return true;
        }
    }
}