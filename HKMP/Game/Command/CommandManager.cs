using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hkmp.Api.Command;

namespace Hkmp.Game.Command {
    /// <summary>
    /// Abstract base class for client and server-side command managers.
    /// </summary>
    public abstract class CommandManager<TCommand> : ICommandManager<TCommand> where TCommand : ICommand {
        /// <summary>
        /// Dictionary mapping command triggers and aliases to their respective commands.
        /// </summary>
        protected readonly Dictionary<string, TCommand> Commands;

        protected CommandManager() {
            Commands = new Dictionary<string, TCommand>();
        }

        protected string[] GetArguments(string message) {
            var argList = new List<string>();

            var argRegex = new Regex("([^\"\\s]\\S*|\".*?\")");
            var matches = argRegex.Matches(message);

            foreach (var match in matches) {
                argList.Add(match.ToString().Replace("\"", ""));
            }

            return argList.ToArray();
        }

        public void RegisterCommand(TCommand command) {
            // Check if the trigger for this command already exists and if so, we return false
            if (Commands.ContainsKey(command.Trigger)) {
                throw new Exception(
                    $"Could not register command: {command.Trigger}, another command under that trigger was already registered");
            }

            Commands[command.Trigger] = command;

            // For each of the aliases we check if it exists and if so, we skip it
            // Aliases are not necessary for correct functioning
            foreach (var alias in command.Aliases) {
                if (Commands.ContainsKey(alias)) {
                    Logger.Get().Warn(this, $"Could not register command alias: {command.Aliases} for command: {command.Trigger}, the alias was already registered");
                    continue;
                }

                Commands[alias] = command;
            }
        }

        public void DeregisterCommand(TCommand command) {
            void DeregisterByName(string commandName, bool shouldThrow) {
                if (!Commands.TryGetValue(commandName, out var registeredCommand)) {
                    var message = $"Could not de-register command: {commandName}, it wasn't registered";
                    if (shouldThrow) {
                        throw new Exception(message);
                    }
                    
                    Logger.Get().Warn(this, message);
                    return;
                }
            
                if (registeredCommand.Equals(command)) {
                    Commands.Remove(commandName);
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