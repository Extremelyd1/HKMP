using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hkmp.Api.Command;
using Hkmp.Logging;

namespace Hkmp.Game.Command;

/// <summary>
/// Abstract base class for client and server-side command managers.
/// </summary>
internal abstract class CommandManager<TCommand> : ICommandManager<TCommand> where TCommand : ICommand {
    /// <summary>
    /// Dictionary mapping command triggers and aliases to their respective commands.
    /// </summary>
    protected readonly Dictionary<string, TCommand> Commands;

    protected CommandManager() {
        Commands = new Dictionary<string, TCommand>();
    }

    /// <summary>
    /// Get the command arguments from the given message. Each string between spaces will be considered as
    /// an argument. Arguments with spaces can be denoted by wrapping them in quotation marks (").
    /// </summary>
    /// <param name="message">The string message.</param>
    /// <returns>A string array containing the arguments.</returns>
    protected string[] GetArguments(string message) {
        var argList = new List<string>();

        var argRegex = new Regex("([^\"\\s]\\S*|\".*?\")");
        var matches = argRegex.Matches(message);

        foreach (var match in matches) {
            argList.Add(match.ToString().Replace("\"", ""));
        }

        return argList.ToArray();
    }

    /// <inheritdoc />
    public void RegisterCommand(TCommand command) {
        // Check if the trigger for this command already exists and if so, we return false
        if (Commands.ContainsKey(command.Trigger)) {
            throw new Exception(
                $"Could not register command: {command.Trigger}, another command under that trigger was already registered");
        }

        foreach (var alias in command.Aliases) {
            if (Commands.ContainsKey(alias)) {
                throw new Exception(
                    $"Could not register command alias: {alias} for command: {command.Trigger}, the alias was already registered");
            }
        }

        Commands[command.Trigger] = command;

        // For each of the aliases we check if it exists and if so, we skip it
        // Aliases are not necessary for correct functioning
        foreach (var alias in command.Aliases) {
            Commands[alias] = command;
        }
    }

    /// <inheritdoc />
    public void DeregisterCommand(TCommand command) {
        void DeregisterByName(string commandName, bool shouldThrow) {
            if (!Commands.TryGetValue(commandName, out var registeredCommand)) {
                var message = $"Could not de-register command: {commandName}, it wasn't registered";
                if (shouldThrow) {
                    throw new Exception(message);
                }

                Logger.Debug(message);
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

                Logger.Debug(message);
            }
        }

        DeregisterByName(command.Trigger, true);

        foreach (var alias in command.Aliases) {
            DeregisterByName(alias, false);
        }
    }
}
