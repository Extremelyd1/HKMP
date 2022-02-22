using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hkmp.Api.Command;

namespace Hkmp.Game.Command {
    /// <summary>
    /// Class that manages commands for both client and server side.
    /// </summary>
    public class CommandManager : ICommandManager {
        /// <summary>
        /// Dictionary mapping command triggers and aliases to their respective commands.
        /// </summary>
        private readonly Dictionary<string, ICommand> _commands;

        public CommandManager() {
            _commands = new Dictionary<string, ICommand>();
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

        public void RegisterCommand(ICommand command) {
            // Check if the trigger for this command already exists and if so, we return false
            if (_commands.ContainsKey(command.Trigger)) {
                throw new Exception(
                    $"Could not register command: {command.Trigger}, another command under that trigger was already registered");
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
        }

        public void DeregisterCommand(ICommand command) {
            void DeregisterByName(string commandName, bool shouldThrow) {
                if (!_commands.TryGetValue(commandName, out var registeredCommand)) {
                    var message = $"Could not de-register command: {commandName}, it wasn't registered";
                    if (shouldThrow) {
                        throw new Exception(message);
                    } else {
                        Logger.Get().Warn(this, message);
                    }
                }
            
                if (registeredCommand == command) {
                    _commands.Remove(commandName);
                } else {
                    var message =
                        $"Could not de-register command: {commandName}, given command did not belong to that name";
                    if (shouldThrow) {
                        throw new Exception(message);
                    }
                    
                    Logger.Get().Warn(this, message);
                }
            }
            
            DeregisterByName(command.Trigger, true);

            foreach (var alias in command.Aliases) {
                DeregisterByName(alias, false);
            }
        }
    }
}