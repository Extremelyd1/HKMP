using Hkmp.Api.Command.Client;

namespace Hkmp.Game.Command.Client;

/// <summary>
/// Class that manages commands for client-side.
/// </summary>
internal class ClientCommandManager : CommandManager<IClientCommand>, IClientCommandManager {
    /// <summary>
    /// Try to process a command given the message.
    /// </summary>
    /// <param name="message">A user-input string message.</param>
    /// <returns>True if the message was processed as a command, false otherwise.</returns>
    public bool ProcessCommand(string message) {
        var arguments = GetArguments(message);

        if (arguments.Length == 0) {
            return false;
        }

        var commandString = arguments[0];
        if (Commands.TryGetValue(commandString, out var command)) {
            command.Execute(arguments);

            return true;
        }

        return false;
    }
}
